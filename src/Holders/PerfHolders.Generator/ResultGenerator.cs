namespace Perf.Holders.Generator;

using System.Text;
using Builders;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Types;

[Generator]
sealed class ResultHolderGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node.IsResultHolder(),
            static ResultHolderContextInfo (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } result) {
                    return default;
                }

                var info = MapContextInfo(context, result);
                return info;
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider.SelectCompInfo(HolderType.Result);

        var resultHolderConfiguration = context.AnalyzerConfigOptionsProvider.ReadResultConfiguration();

        var final = filtered.Combine(resultHolderConfiguration).Combine(compInfo);

        context.RegisterSourceOutput(
            final,
            static (context, final) => {
                var ((resultInfo, resultConfiguration), compInfo) = final;
                if (compInfo == default) {
                    return;
                }

                resultInfo = resultInfo with {
                    Configuration = resultConfiguration.MergeWithMajor(resultInfo.Configuration).ApplyDefaults()
                };

                var sourceText = new ResultSourceBuilder(resultInfo, compInfo).WriteAllAndBuild();

                var fileName = $"{resultInfo.MetadataName}.g.cs";

                context.AddSource(fileName, SourceText.From(sourceText, Encoding.UTF8));

                if (resultInfo.ShouldGenerateJsonConverters()) {
                    var sourceStj = new ResultSystemTextJsonSourceBuilder(resultInfo, compInfo).WriteAllAndBuild();
                    var fileNameStj = $"{resultInfo.MetadataName}.g.Stj.cs";
                    context.AddSource(fileNameStj, SourceText.From(sourceStj, Encoding.UTF8));
                }

                if (resultInfo.ShouldGenerateMessagePackFormatters()) {
                    var sourceMsgPack = new ResultMessagePackSourceBuilder(resultInfo, compInfo).WriteAllAndBuild();
                    var fileNameMsgPack = $"{resultInfo.MetadataName}.g.MsgPack.cs";
                    context.AddSource(fileNameMsgPack, SourceText.From(sourceMsgPack, Encoding.UTF8));
                }
            }
        );
    }

    static ResultHolderContextInfo MapContextInfo(GeneratorSyntaxContext context, INamedTypeSymbol result) {
        INamedTypeSymbol marker = null!;
        foreach (var i in result.Interfaces) {
            var iPath = i.FullPath();

            if (iPath is not HolderTypeNames.ResultMarkerFullName) {
                if (iPath.AsSpan().StartsWith("Perf.Holders.".AsSpan(), StringComparison.Ordinal)) {
                    return default;
                }

                continue;
            }

            if (marker is not null) {
                return default;
            }

            marker = i;
        }

        if (marker is null) {
            return default;
        }

        var okArg = marker.TypeArguments[0];
        var errorArg = marker.TypeArguments[1];

        if (okArg.Equals(errorArg, SymbolEqualityComparer.Default)) {
            return default;
        }

        if (okArg.NullableAnnotation is NullableAnnotation.Annotated) {
            return default;
        }

        var okArgNullable = okArg.MakeNullable(context.SemanticModel.Compilation);
        var errorArgNullable = errorArg.MakeNullable(context.SemanticModel.Compilation);

        var resultInfo = new ResultHolderContextInfo.ResultInfo(
            DeclarationName: result.MinimalName(),
            OnlyName: result.Name,
            GlobalName: result.GlobalName(),
            Accessibility: result.DeclaredAccessibility.MapToTypeAccessibility()
        );
        if (resultInfo.Accessibility is TypeAccessibility.None) {
            return default;
        }

        var okInfo = new ResultHolderContextInfo.OkInfo(
            IsStruct: okArg.IsValueType,
            IsTypeParameter: okArg is ITypeParameterSymbol,
            Property: ResultHolderContextInfo.OkInfo.DefaultProperty,
            Field: ResultHolderContextInfo.OkInfo.DefaultField,
            Type: okArg.GlobalName(),
            TypeNullable: okArgNullable.GlobalName(),
            HavePartial: false
        );
        var errorInfo = new ResultHolderContextInfo.ErrorInfo(
            IsStruct: errorArg.IsValueType,
            IsTypeParameter: errorArg is ITypeParameterSymbol,
            Property: ResultHolderContextInfo.ErrorInfo.DefaultProperty,
            Field: ResultHolderContextInfo.ErrorInfo.DefaultField,
            Type: errorArg.GlobalName(),
            TypeNullable: errorArgNullable.GlobalName(),
            HavePartial: false
        );
        var isOkInfo = new ResultHolderContextInfo.IsOkInfo(
            Property: ResultHolderContextInfo.IsOkInfo.DefaultProperty,
            HavePartial: false
        );

        var declaredPartialProperties = result.GetMembers().WhereOfType<IPropertySymbol>(x => x is {
                DeclaredAccessibility: Accessibility.Public,
                IsPartialDefinition: true
            }
        );
        if (declaredPartialProperties.Length > 0) {
            foreach (var ps in declaredPartialProperties) {
                if (okInfo.HavePartial is false && ps.Type.Equals(okArg, SymbolEqualityComparer.Default)) {
                    okInfo = okInfo with {
                        Property = ps.Name,
                        Field = ps.Name.ToFieldFormat(),
                        HavePartial = true
                    };
                } else if (errorInfo.HavePartial is false && ps.Type.Equals(errorArg, SymbolEqualityComparer.Default)) {
                    errorInfo = errorInfo with {
                        Property = ps.Name,
                        Field = ps.Name.ToFieldFormat(),
                        HavePartial = true
                    };
                } else if (isOkInfo.HavePartial is false && ps.Type is INamedTypeSymbol { SpecialType: SpecialType.System_Boolean }) {
                    isOkInfo = new(
                        Property: ps.Name,
                        HavePartial: true
                    );
                }
            }

            if (okInfo.HavePartial && isOkInfo.HavePartial is false) {
                isOkInfo = new(
                    Property: $"Is{okInfo.Property}",
                    HavePartial: false
                );
            }
        }

        var containingTypes = result.GetContainingTypeList();
        var configuration = result.GetAttributes().ReadResultConfigurationFromAttributes();

        return new(
            MetadataName: result.MetadataName,
            Namespace: result.GetNamespaceString(),
            Result: resultInfo,
            Ok: okInfo,
            Error: errorInfo,
            IsOk: isOkInfo,
            ContainingTypes: containingTypes,
            Configuration: configuration
        );
    }
}
