namespace Perf.Holders.Generator;

using System.Text;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                    if (iPath is HolderTypeNames.ResultMarkerFullName) {
                        return default;
                    }

                    if (iPath is not HolderTypeNames.OptionMarkerFullName) {
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

                var arg = marker.TypeArguments[0];
                if (arg.NullableAnnotation is NullableAnnotation.Annotated) {
                    return default;
                }

                var argNullable = arg.WithNullableAnnotation(NullableAnnotation.Annotated);
                var argName = arg.GlobalName();
                var argNullableName = argNullable.GlobalName();

                var patternValues = new Dictionary<string, string?> {
                    ["NamespaceDeclaration"] = option.ContainingNamespace.IsGlobalNamespace is false
                        ? $"namespace {option.ContainingNamespace.ToDisplayString()}\n"
                        : "",
                    ["OptionName"] = option.Name,
                    ["OptionShort"] = option.MinimalName(),
                    ["OptionType"] = option.GlobalName(),
                    ["TypeArguments"] = option.TypeArguments switch {
                        [ ITypeParameterSymbol ] => $"<{argName}>",
                        _                        => null
                    },
                    ["TypeArgumentsConstraints"] = option.TypeArguments switch {
                        [ ITypeParameterSymbol ] => $"where {argName} : notnull ",
                        _                        => null
                    },
                    ["OpenTypeArguments"] = option.TypeArguments switch {
                        [ _ ] => "<>",
                        _     => null
                    },
                    ["IsSomeProperty"] = "IsSome",
                    ["IsSomeInterfaceProperty"] = "",
                    ["IsSomeDeclarationModifiers"] = "",
                    ["SomeProperty"] = "Some",
                    ["SomeField"] = "some",
                    ["SomeInterfaceProperty"] = "",
                    ["SomeDeclarationModifiers"] = "",
                    ["SomeType"] = argName,
                    ["SomeTypeForEquals"] = arg switch {
                        { IsReferenceType: true } => argNullableName,
                        _                         => argName
                    },
                    ["SomeTypeNullable"] = argNullableName,
                    ["OptionState"] = "global::Perf.Holders.OptionState",
                    ["BaseOption"] = "global::Perf.Holders.Option",
                    ["DebuggerBrowsableNever"] =
                        "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                };

                var declaredPartialProperties = option.GetMembers()
                    .WhereOfType<IPropertySymbol>(x => x.IsPartialDefinition);
                if (declaredPartialProperties.Length > 0) {
                    var someSet = false;
                    var isSomeSet = false;

                    foreach (var ps in declaredPartialProperties) {
                        if (someSet is false && ps.Type.Equals(arg, SymbolEqualityComparer.Default)) {
                            someSet = true;
                            patternValues["SomeProperty"] = ps.Name;
                            patternValues["SomeField"] = ps.Name.ToFieldFormat();
                            patternValues["SomeDeclarationModifiers"] = "partial ";
                            if (ps.Name is not "Some") {
                                patternValues["SomeInterfaceProperty"] = $"""
                                        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
                                        {argName} global::Perf.Holders.IOptionHolder<{argName}>.Some => {ps.Name};
                                    """;
                            }
                        } else if (isSomeSet is false && ps.Type is INamedTypeSymbol { SpecialType: SpecialType.System_Boolean }) {
                            isSomeSet = true;
                            patternValues["IsSomeProperty"] = ps.Name;
                            patternValues["IsSomeDeclarationModifiers"] = "partial ";
                            if (ps.Name is not "IsSome") {
                                patternValues["IsSomeInterfaceProperty"] = $"""
                                        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
                                        bool global::Perf.Holders.IOptionHolder<{argName}>.IsSome => {ps.Name};
                                    """;
                            }
                        }
                    }

                    if (isSomeSet is false && someSet) {
                        patternValues["IsSomeProperty"] = $"Is{patternValues["SomeProperty"]}";
                        patternValues["IsSomeInterfaceProperty"] = $"""
                                bool global::Perf.Holders.IOptionHolder<{argName}>.IsSome => Is{patternValues["SomeProperty"]};
                            """;
                    }
                }

                return new BasicHolderContextInfo(
                    MinimalNameWithGenericMetadata: MinimalNameWithGenericMetadata(option),
                    PatternValues: patternValues
                );
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider
            .Select(static (c, _) => {
                    LanguageVersion? langVersion = c is CSharpCompilation comp ? comp.LanguageVersion : null;
                    return new CompInfo(langVersion);
                }
            );

        var typesAndCompInfo = filtered.Combine(compInfo);

        context.RegisterSourceOutput(
            typesAndCompInfo,
            static (context, tuple1) => {
                var ((minimalNameWithGenericMetadata, values), compInfo) = tuple1;

                values["DebugViewVisibility"] = compInfo.Version is >= LanguageVersion.CSharp11
                    ? "file "
                    : "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n";
                values["NullableFileAnnotation"] = compInfo.Version is >= LanguageVersion.CSharp8
                    ? "#nullable enable"
                    : "";

                var sourceText = PatternFormatter.Format(Patterns.Option, values);

#if DEBUG
                context.AddSource($"{minimalNameWithGenericMetadata}.cs", SourceText.From(sourceText, Encoding.UTF8));
#else
                context.AddSource($"{minimalNameWithGenericMetadata}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
#endif
            }
        );
    }

    static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
