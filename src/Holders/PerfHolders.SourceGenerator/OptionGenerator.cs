namespace Perf.Holders.Generator;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class OptionHolderGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, ct) => {
                if (node is not StructDeclarationSyntax {
                    BaseList.Types.Count: > 0
                } s) {
                    return false;
                }

                if (s.Modifiers.Any(SyntaxKind.PartialKeyword) is false) {
                    return false;
                }

                foreach (var bt in s.BaseList.Types) {
                    if (bt is SimpleBaseTypeSyntax {
                        Type: QualifiedNameSyntax {
                            Right: GenericNameSyntax {
                                Identifier.Text: "IOptionHolder",
                                TypeArgumentList.Arguments.Count: 1
                            }
                        }
                    }) {
                        return true;
                    }
                    if (bt is {
                        Type: GenericNameSyntax {
                            Identifier.Text : "IOptionHolder",
                            TypeArgumentList.Arguments.Count: 1
                        }
                    }) {
                        return true;
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
                    return default;
                }

                INamedTypeSymbol marker = null!;
                foreach (var i in symbol.Interfaces) {
                    if (i.FullPath() is Constants.OptionInterfaceFullName) {
                        if (marker != null) {
                            return default;
                        }

                        marker = i;
                    }
                }

                var arg = marker.TypeArguments[0];

                var patternValues = new Dictionary<string, string?>(16) {
                    ["Namespace"] = symbol.ContainingNamespace.ToDisplayString(),
                    ["OptionName"] = symbol.Name,
                    ["OptionShort"] = symbol.MinimalName(),
                    ["OptionTypeofString"] = symbol.TypeArguments switch {
                        []      => symbol.Name,
                        [var t] => $"{symbol.Name}<{{typeof({t.MinimalName()}).Name}}>",
                        _       => null
                    },
                    ["TypeArguments"] = symbol.TypeArguments switch {
                        [ITypeParameterSymbol t1] => $"<{t1.Name}>",
                        [{ } t1]                  => $"<{t1.MinimalName()}>",
                        _                         => null
                    },
                    ["OpenTypeArguments"] = symbol.TypeArguments.Length switch {
                        1 => "<>",
                        _ => null
                    },
                    ["SomeQualifiedForEquals"] = arg switch {
                        ITypeParameterSymbol t    => t.Name,
                        { IsReferenceType: true } => $"{arg.GlobalName()}?",
                        _                         => arg.GlobalName()
                    },
                    ["SomeQualified"] = arg is ITypeParameterSymbol ? arg.Name : arg.GlobalName(),
                    ["OptionStateQualified"] = "global::Perf.Holders.OptionState",
                    ["SharedOptionQualified"] = "global::Perf.Holders.Option"
                };

                return new BasicHolderContextInfo(
                    MinimalNameWithGenericMetadata: MinimalNameWithGenericMetadata(symbol),
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
                var (holderInfo, compInfo) = tuple1;

                holderInfo.PatternValues["DebugViewVisibility"] = compInfo.Version is >= LanguageVersion.CSharp11
                    ? "file "
                    : "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n";

                var sourceText = PatternFormatter.Format(
                    Patterns.Option1,
                    holderInfo.PatternValues
                );

                context.AddSource($"{holderInfo.MinimalNameWithGenericMetadata}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        );
    }

    static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
