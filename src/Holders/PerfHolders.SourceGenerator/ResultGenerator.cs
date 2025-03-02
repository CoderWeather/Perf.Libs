namespace Perf.Holders.Generator;

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
sealed class ResultHolderGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => {
                if (node is not StructDeclarationSyntax {
                        Modifiers.Count: > 0,
                        BaseList.Types.Count: > 0,
                        TypeParameterList: null or { Parameters.Count: <= 2 }
                    } sds
                    || sds.Modifiers.Any(SyntaxKind.PartialKeyword) is false
                    || sds.Modifiers.Any(SyntaxKind.RefKeyword)
                ) {
                    return false;
                }

                foreach (var bt in sds.BaseList.Types) {
                    switch (bt) {
                        case SimpleBaseTypeSyntax {
                            Type: QualifiedNameSyntax {
                                Right: GenericNameSyntax {
                                    Identifier.Text: "IResultHolder",
                                    TypeArgumentList.Arguments.Count: 2
                                }
                            }
                        }:
                        case SimpleBaseTypeSyntax {
                            Type: GenericNameSyntax {
                                Identifier.Text: "IResultHolder",
                                TypeArgumentList.Arguments.Count: 2
                            }
                        }:
                            return true;
                        default:
                            continue;
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } result) {
                    return default;
                }

                INamedTypeSymbol marker = null!;
                foreach (var i in result.Interfaces) {
                    var iPath = i.FullPath();
                    if (iPath is Constants.OptionMarkerFullName) {
                        return default;
                    }

                    if (iPath is not Constants.ResultMarkerFullName) {
                        continue;
                    }

                    if (marker != null) {
                        return default;
                    }

                    marker = i;
                }

                var arg1 = marker.TypeArguments[0];
                var arg1Name = arg1.GlobalName();
                var arg2 = marker.TypeArguments[1];
                var arg2Name = arg2.GlobalName();

                if (arg1.Equals(arg2, SymbolEqualityComparer.Default)) {
                    return default;
                }

                var patternValues = new Dictionary<string, string?> {
                    ["Namespace"] = result.ContainingNamespace.IsGlobalNamespace is false
                        ? result.ContainingNamespace.ToDisplayString()
                        : context.SemanticModel.Compilation.Assembly.Name,
                    ["ResultName"] = result.Name,
                    ["ResultShort"] = result.MinimalName(),
                    ["ResultTypeofString"] = result.TypeArguments switch {
                        [ ]      => result.Name,
                        [ _ ]    => $"{result.Name}<{{typeof({arg1Name}).Name}}>",
                        [ _, _ ] => $"{result.Name}<{{typeof({arg1Name}).Name}}, {{typeof({arg2Name}).Name}}>",
                        _        => result.MinimalName()
                    },
                    ["TypeArguments"] = result.TypeArguments switch {
                        [ ITypeParameterSymbol, ITypeParameterSymbol ] => $"<{arg1Name}, {arg2Name}>",
                        [ ITypeParameterSymbol ]                       => $"<{arg1Name}>",
                        _                                              => null
                    },
                    ["TypeArgumentsConstraints"] = result.TypeArguments switch {
                        [ ITypeParameterSymbol, ITypeParameterSymbol ] => $"where {arg1Name} : notnull where {arg2Name} : notnull ",
                        [ ITypeParameterSymbol ]                       => $"where {arg1Name} : notnull ",
                        _                                              => null
                    },
                    ["OpenTypeArguments"] = result.TypeArguments switch {
                        [ _ ]    => "<>",
                        [ _, _ ] => "<,>",
                        _        => null
                    },
                    ["IsOkProperty"] = "IsOk",
                    ["IsOkDeclarationModifiers"] = " ",
                    ["OkProperty"] = "Ok",
                    ["OkField"] = "ok",
                    ["OkDeclarationModifiers"] = " ",
                    ["OkType"] = arg1Name,
                    ["OkTypeForEquals"] = arg1 switch {
                        ITypeParameterSymbol or { IsReferenceType: true } => $"{arg1Name}?",
                        _                                                 => arg1Name
                    },
                    ["ErrorProperty"] = "Error",
                    ["ErrorField"] = "error",
                    ["ErrorDeclarationModifiers"] = " ",
                    ["ErrorType"] = arg2Name,
                    ["ErrorTypeForEquals"] = arg2 switch {
                        ITypeParameterSymbol or { IsReferenceType: true } => $"{arg2Name}?",
                        _                                                 => arg2Name
                    },
                    ["ResultState"] = "global::Perf.Holders.ResultState",
                    ["SharedResultQualified"] = "global::Perf.Holders.Result"
                };

                var declaredPartialProperties = GetPropertiesWithPredicate(result.GetMembers(), x => x.IsPartialDefinition);
                if (declaredPartialProperties.Length > 0) {
                    var isOkSet = false;
                    var okSet = false;
                    var errorSet = false;
                    foreach (var sp in declaredPartialProperties) {
                        if (okSet is false && sp.Type.Equals(arg1, SymbolEqualityComparer.Default)) {
                            patternValues["OkProperty"] = sp.Name;
                            patternValues["OkField"] = sp.Name.ToFieldFormat();
                            patternValues["OkDeclarationModifiers"] = "partial ";
                            okSet = true;
                            if (isOkSet is false) {
                                patternValues["IsOkProperty"] = $"Is{sp.Name}";
                            }
                        } else if (errorSet is false && sp.Type.Equals(arg2, SymbolEqualityComparer.Default)) {
                            patternValues["ErrorProperty"] = sp.Name;
                            patternValues["ErrorField"] = sp.Name.ToFieldFormat();
                            patternValues["ErrorDeclarationModifiers"] = "partial ";
                            errorSet = true;
                        } else if (isOkSet is false && sp.Type is INamedTypeSymbol { SpecialType: SpecialType.System_Boolean }) {
                            patternValues["IsOkProperty"] = sp.Name;
                            patternValues["IsOkDeclarationModifiers"] = "partial ";
                            isOkSet = true;
                        }
                    }
                }

                return new BasicHolderContextInfo(
                    MinimalNameWithGenericMetadata: MinimalNameWithGenericMetadata(result),
                    PatternValues: patternValues
                );
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider
            .Select(
                static (c, _) => {
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
                values["NullableFileAnnotation"] = compInfo.Version is >= LanguageVersion.CSharp6
                    ? "#nullable enable\n"
                    : "";

                var sourceText = PatternFormatter.Format(
                    Patterns.Result,
                    values
                );

                context.AddSource($"{minimalNameWithGenericMetadata}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        );
    }

    static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }

    static ImmutableArray<IPropertySymbol> GetPropertiesWithPredicate(ImmutableArray<ISymbol> symbols, Func<IPropertySymbol, bool> predicate) {
        var count = 0;
        var span = symbols.AsSpan();
        foreach (ref readonly var sr in span) {
            if (sr is IPropertySymbol ps && predicate(ps)) {
                count++;
            }
        }

        if (count is 0) {
            return ImmutableArray<IPropertySymbol>.Empty;
        }

        var results = new IPropertySymbol[count];
        var i = 0;
        foreach (ref readonly var sr in span) {
            if (sr is IPropertySymbol ps) {
                results[i++] = ps;
            }
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(results);
    }
}
