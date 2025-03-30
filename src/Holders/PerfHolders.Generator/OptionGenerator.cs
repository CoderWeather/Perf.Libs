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

                EquatableList<HolderContainingType> containingTypes = default;
                if (option.ContainingType != null) {
                    containingTypes = [ ];
                    var t = option;
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

                var arg = marker.TypeArguments[0];
                if (arg.NullableAnnotation is NullableAnnotation.Annotated) {
                    return default;
                }

                var configuration = option.GetAttributes().ReadOptionConfigurationFromAttributes();

                var argNullable = arg.IsValueType
                    ? context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(arg)
                    : arg.WithNullableAnnotation(NullableAnnotation.Annotated);

                var optionInfo = new OptionHolderContextInfo.OptionInfo(
                    DeclarationName: option.MinimalName(),
                    OnlyName: option.Name,
                    GlobalName: option.GlobalName(),
                    TypeArgumentCount: option.TypeArguments.Length,
                    Accessibility: option.DeclaredAccessibility switch {
                        Accessibility.Public   => TypeAccessibility.Public,
                        Accessibility.Private  => TypeAccessibility.Private,
                        Accessibility.Internal => TypeAccessibility.Internal,
                        _                      => TypeAccessibility.None
                    }
                );
                if (optionInfo.Accessibility is TypeAccessibility.None) {
                    return default;
                }

                var someInfo = new OptionHolderContextInfo.SomeInfo(
                    IsStruct: arg.IsValueType,
                    IsTypeArgument: arg is ITypeParameterSymbol,
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

                return new OptionHolderContextInfo(
                    SourceFileName: SourceFileName(option),
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

        var compInfo = context.CompilationProvider.SelectCompInfo();

        var optionHolderConfiguration = context.AnalyzerConfigOptionsProvider.ReadOptionConfiguration();

        var final = filtered.Combine(compInfo).Combine(optionHolderConfiguration);

        context.RegisterSourceOutput(
            final,
            static (context, tuple1) => {
                var ((optionInfo, compInfo), optionConfiguration) = tuple1;
                var sourceFileName = optionInfo.SourceFileName;

                optionInfo = optionInfo with {
                    Configuration = optionInfo.Configuration.MergeWithMajor(optionConfiguration).ApplyDefaults()
                };

                var sourceText = new OptionSourceBuilder(optionInfo, compInfo).WriteAllAndBuild();

                if (compInfo.OptimizationLevel is OptimizationLevel.Debug) {
                    context.AddSource($"{sourceFileName}.cs", SourceText.From(sourceText, Encoding.UTF8));
                } else if (compInfo.OptimizationLevel is OptimizationLevel.Release) {
                    context.AddSource($"{sourceFileName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        );
    }

    static string SourceFileName(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
