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
sealed class MultiResultGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node.IsMultiResultHolder(),
            static MultiResultHolderContextInfo (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } multiResult) {
                    return default;
                }

                var info = MapContextInfo(context, multiResult);
                return info;
            }
        );

        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider.SelectCompInfo(HolderType.MultiResult);

        var multiResultHolderConfiguration = context.AnalyzerConfigOptionsProvider.ReadMultiResultConfiguration();

        var final = filtered.Combine(multiResultHolderConfiguration).Combine(compInfo);

        context.RegisterSourceOutput(
            final,
            static (context, final) => {
                var ((mrInfo, mrConfiguration), compInfo) = final;
                if (compInfo == default) {
                    return;
                }

                mrInfo = mrInfo with {
                    Configuration = mrConfiguration.MergeWithMajor(mrInfo.Configuration).ApplyDefaults()
                };

                var sourceText = new MultiResultSourceBuilder(mrInfo, compInfo).WriteAllAndBuild();

                var fileName = $"{mrInfo.MetadataName}.g.cs";

                context.AddSource(fileName, SourceText.From(sourceText, Encoding.UTF8));

                if (mrInfo.ShouldGenerateJsonConverters()) {
                    var sourceStj = new MultiResultSystemTextJsonSourceBuilder(mrInfo, compInfo).WriteAllAndBuild();
                    var fileNameStj = $"{mrInfo.MetadataName}.stj.g.cs";
                    context.AddSource(fileNameStj, SourceText.From(sourceStj, Encoding.UTF8));
                }

                if (mrInfo.ShouldGenerateMessagePackFormatters()) {
                    var sourceMsgPack = new MultiResultMessagePackSourceBuilder(mrInfo, compInfo).WriteAllAndBuild();
                    var fileNameMsgPack = $"{mrInfo.MetadataName}.msgpack.g.cs";
                    context.AddSource(fileNameMsgPack, SourceText.From(sourceMsgPack, Encoding.UTF8));
                }
            }
        );
    }

    static MultiResultHolderContextInfo MapContextInfo(GeneratorSyntaxContext context, INamedTypeSymbol multiResult) {
        INamedTypeSymbol marker = null!;
        foreach (var i in multiResult.Interfaces) {
            var iPath = i.FullPath();

            if (iPath is not HolderTypeNames.MultiResultMarkerFullName) {
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

        var mrInfo = new MultiResultHolderContextInfo.MultiResultInfo(
            DeclarationName: multiResult.MinimalName(),
            OnlyName: multiResult.Name,
            GlobalName: multiResult.GlobalName(),
            TypeParameterCount: multiResult.TypeArguments.Length,
            Accessibility: multiResult.DeclaredAccessibility.MapToTypeAccessibility()
        );
        if (mrInfo.Accessibility is TypeAccessibility.None) {
            return default;
        }

        // TODO: check getting type parameters strings from INamedTypeSymbol
        EquatableList<MultiResultHolderContextInfo.MultiResultElementInfo> elements = new(multiResult.TypeArguments.Length);
        for (var index = 0; index < marker.TypeArguments.Length; index++) {
            var elTs = marker.TypeArguments[index];
            if (elTs.NullableAnnotation is NullableAnnotation.Annotated) {
                return default;
            }

            var elType = elTs.GlobalName();
            if (elements.Any(x => x.Type.Equals(elType))) {
                return default;
            }

            var el = new MultiResultHolderContextInfo.MultiResultElementInfo(
                Index: index,
                IsStruct: elTs.IsValueType,
                IsTypeParameter: elTs is ITypeParameterSymbol,
                Property: MultiResultHolderContextInfo.MultiResultElementInfo.Properties[index],
                Field: MultiResultHolderContextInfo.MultiResultElementInfo.Fields[index],
                Type: elTs.GlobalName(),
                TypeNullable: elTs.MakeNullable(context.SemanticModel.Compilation).GlobalName(),
                HavePartial: false,
                StateCheck: new(
                    Property: $"Is{MultiResultHolderContextInfo.MultiResultElementInfo.Properties[index]}",
                    HavePartial: false
                )
            );

            elements.Add(el);
        }

        var declaredPartialProperties = multiResult.GetMembers()
            .WhereOfType<IPropertySymbol>(x => x is { DeclaredAccessibility: Accessibility.Public, IsPartialDefinition: true });
        for (var psIndex = 0; psIndex < declaredPartialProperties.Length; psIndex++) {
            var ps = declaredPartialProperties[psIndex];
            var psType = ps.Type.GlobalName();
            for (var elementIndex = 0; elementIndex < elements.Count; elementIndex++) {
                var el = elements[elementIndex];
                if (el.HavePartial is false && el.Type.Equals(psType)) {
                    var stateCheck = elements[elementIndex].StateCheck;
                    if (psIndex < declaredPartialProperties.Length - 1) {
                        var nextPs = declaredPartialProperties[psIndex + 1];
                        if (nextPs.Type.SpecialType is SpecialType.System_Boolean) {
                            psIndex++;
                            stateCheck = new(Property: nextPs.Name, HavePartial: true);
                        }
                    }

                    elements[elementIndex] = el with {
                        Property = ps.Name,
                        Field = ps.Name.ToFieldFormat(),
                        HavePartial = true,
                        StateCheck = stateCheck
                    };
                    break;
                }
            }
        }

        var containingTypes = multiResult.GetContainingTypeList();
        var configuration = multiResult.GetAttributes().ReadMultiResultConfigurationFromAttributes();

        return new(
            MetadataName: multiResult.MetadataName,
            MultiResult: mrInfo,
            Elements: elements,
            Namespace: multiResult.GetNamespaceString(),
            ContainingTypes: containingTypes,
            Configuration: configuration
        );
    }
}
