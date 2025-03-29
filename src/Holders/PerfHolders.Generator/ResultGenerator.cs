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
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } result) {
                    return default;
                }

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

                EquatableList<HolderContainingType> containingTypes = default;
                if (result.ContainingType != null) {
                    containingTypes = [ ];
                    var t = result;
                    do {
                        t = t.ContainingType;

                        HolderContainingType containingType = t switch {
                            { IsReferenceType: true, IsRecord: true } => new(Kind: "record", Name: t.Name),
                            { IsReferenceType: true }                 => new(Kind: "class", Name: t.Name),
                            { IsValueType: true, IsRecord: true }     => new(Kind: "record struct", Name: t.Name),
                            { IsValueType: true }                     => new(Kind: "struct", Name: t.Name),
                            _                                         => default
                        };
                        if (containingType != default) {
                            containingTypes.Add(containingType);
                        }
                    } while (t.ContainingType != null);
                }

                var okArg = marker.TypeArguments[0];
                var errorArg = marker.TypeArguments[1];

                if (okArg.Equals(errorArg, SymbolEqualityComparer.Default)) {
                    return default;
                }

                if (okArg.NullableAnnotation is NullableAnnotation.Annotated) {
                    return default;
                }

                var configuration = result.GetAttributes().ReadResultConfigurationFromAttributes();

                var okArgNullable = okArg.IsValueType
                    ? context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(okArg)
                    : okArg.WithNullableAnnotation(NullableAnnotation.Annotated);
                var errorArgNullable = errorArg.IsValueType
                    ? context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(errorArg)
                    : errorArg.WithNullableAnnotation(NullableAnnotation.Annotated);

                var resultInfo = new ResultHolderContextInfo.ResultInfo(
                    DeclarationName: result.MinimalName(),
                    OnlyName: result.Name,
                    GlobalName: result.GlobalName(),
                    TypeArgumentCount: result.TypeArguments.Length
                );
                var okInfo = new ResultHolderContextInfo.OkInfo(
                    IsStruct: okArg.IsValueType,
                    IsTypeArgument: okArg is ITypeParameterSymbol,
                    Property: ResultHolderContextInfo.OkInfo.DefaultProperty,
                    Field: ResultHolderContextInfo.OkInfo.DefaultField,
                    Type: okArg.GlobalName(),
                    TypeNullable: okArgNullable.GlobalName(),
                    HavePartial: false
                );
                var errorInfo = new ResultHolderContextInfo.ErrorInfo(
                    IsStruct: errorArg.IsValueType,
                    IsTypeArgument: errorArg is ITypeParameterSymbol,
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

                return new ResultHolderContextInfo(
                    SourceFileName: SourceFileName(result),
                    Namespace: result.ContainingNamespace.IsGlobalNamespace
                        ? null
                        : result.ContainingNamespace.ToDisplayString(),
                    Result: resultInfo,
                    Ok: okInfo,
                    Error: errorInfo,
                    IsOk: isOkInfo,
                    ContainingTypes: containingTypes,
                    Configuration: configuration
                );
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider.SelectCompInfo();

        var resultHolderConfiguration = context.AnalyzerConfigOptionsProvider.ReadResultConfiguration();

        var final = filtered.Combine(compInfo).Combine(resultHolderConfiguration);

        context.RegisterSourceOutput(
            final,
            static (context, tuple1) => {
                var ((resultInfo, compInfo), resultConfiguration) = tuple1;

                resultInfo = resultInfo with {
                    Configuration = resultInfo.Configuration.MergeWithMajor(resultConfiguration).ApplyDefaults()
                };

                var sourceText = new ResultSourceBuilder(resultInfo, compInfo).WriteAllAndBuild();

                if (compInfo.OptimizationLevel is OptimizationLevel.Debug) {
                    context.AddSource($"{resultInfo.SourceFileName}.cs", SourceText.From(sourceText, Encoding.UTF8));
                } else if (compInfo.OptimizationLevel is OptimizationLevel.Release) {
                    context.AddSource($"{resultInfo.SourceFileName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        );
    }

    static string SourceFileName(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
