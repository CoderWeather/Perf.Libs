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
sealed class OptionHolderGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node.IsOptionHolder(),
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } option) {
                    return default;
                }

                INamedTypeSymbol marker = null!;
                foreach (var i in option.Interfaces) {
                    var iPath = i.FullPath();

                    if (iPath is not HolderTypeNames.OptionMarkerFullName) {
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
                // check for type arguments zero or one and this one should be argument for marker

                var arg = marker.TypeArguments[0];
                if (arg.NullableAnnotation is NullableAnnotation.Annotated) {
                    return default;
                }

                var argNullable = arg.MakeNullable(context.SemanticModel.Compilation);

                var optionInfo = new OptionHolderContextInfo.OptionInfo(
                    DeclarationName: option.MinimalName(),
                    OnlyName: option.Name,
                    GlobalName: option.GlobalName(),
                    TypeParameterCount: option.TypeArguments.Length,
                    Accessibility: option.DeclaredAccessibility.MapToTypeAccessibility()
                );
                if (optionInfo.Accessibility is TypeAccessibility.None) {
                    return default;
                }

                var someInfo = new OptionHolderContextInfo.SomeInfo(
                    IsStruct: arg.IsValueType,
                    IsTypeParameter: arg is ITypeParameterSymbol,
                    Property: OptionHolderContextInfo.SomeInfo.DefaultProperty,
                    Field: OptionHolderContextInfo.SomeInfo.DefaultField,
                    Type: arg.GlobalName(),
                    TypeNullable: argNullable.GlobalName(),
                    HavePartial: false
                );
                var isSomeInfo = new OptionHolderContextInfo.IsSomeInfo(
                    Property: OptionHolderContextInfo.IsSomeInfo.DefaultProperty,
                    HavePartial: false
                );

                var declaredPartialProperties = option.GetMembers().WhereOfType<IPropertySymbol>(x => x is {
                        DeclaredAccessibility: Accessibility.Public,
                        IsPartialDefinition: true
                    }
                );
                if (declaredPartialProperties.Length > 0) {
                    foreach (var ps in declaredPartialProperties) {
                        if (someInfo.HavePartial is false && ps.Type.Equals(arg, SymbolEqualityComparer.Default)) {
                            someInfo = someInfo with {
                                Property = ps.Name,
                                Field = ps.Name.ToFieldFormat(),
                                HavePartial = true
                            };
                        } else if (isSomeInfo.HavePartial is false && ps.Type is INamedTypeSymbol { SpecialType: SpecialType.System_Boolean }) {
                            isSomeInfo = new(
                                Property: ps.Name,
                                HavePartial: true
                            );
                        }
                    }

                    if (someInfo.HavePartial && isSomeInfo.HavePartial is false) {
                        isSomeInfo = new(
                            Property: $"Is{someInfo.Property}",
                            HavePartial: false
                        );
                    }
                }

                var containingTypes = option.GetContainingTypeList();
                var configuration = option.GetAttributes().ReadOptionConfigurationFromAttributes();

                return new OptionHolderContextInfo(
                    MetadataName: option.MetadataName,
                    Namespace: option.ContainingNamespace.IsGlobalNamespace
                        ? null
                        : option.ContainingNamespace.ToDisplayString(),
                    Option: optionInfo,
                    Some: someInfo,
                    IsSome: isSomeInfo,
                    ContainingTypes: containingTypes,
                    Configuration: configuration
                );
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider.SelectCompInfo(HolderType.Option);

        var optionHolderConfiguration = context.AnalyzerConfigOptionsProvider.ReadOptionConfiguration();

        var final = filtered.Combine(optionHolderConfiguration).Combine(compInfo);

        context.RegisterSourceOutput(
            final,
            static (context, final) => {
                var ((optionInfo, optionConfiguration), compInfo) = final;
                if (compInfo == default) {
                    return;
                }

                optionInfo = optionInfo with {
                    Configuration = optionConfiguration.MergeWithMajor(optionInfo.Configuration).ApplyDefaults()
                };

                var sourceText = new OptionSourceBuilder(optionInfo, compInfo).WriteAllAndBuild();

                var fileName = $"{optionInfo.MetadataName}.g.cs";

                context.AddSource(fileName, SourceText.From(sourceText, Encoding.UTF8));
            }
        );
    }
}
