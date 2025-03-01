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
            static (node, _) => {
                if (node is not StructDeclarationSyntax {
                        BaseList.Types.Count: > 0,
                        TypeParameterList: null
                    } s) {
                    return false;
                }

                if (s.Modifiers.Any(SyntaxKind.PartialKeyword) is false) {
                    return false;
                }

                foreach (var bt in s.BaseList.Types) {
                    switch (bt) {
                        case SimpleBaseTypeSyntax {
                            Type: QualifiedNameSyntax {
                                Right: GenericNameSyntax {
                                    Identifier.Text : "IOptionHolder",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                        }:
                        case {
                            Type: GenericNameSyntax {
                                Identifier.Text : "IOptionHolder",
                                TypeArgumentList.Arguments.Count: 1
                            }
                        }:
                            return true;
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } option) {
                    return default;
                }

                INamedTypeSymbol marker = null!;
                foreach (var i in option.Interfaces) {
                    if (i.FullPath() is Constants.OptionInterfaceFullName) {
                        if (marker is not null) {
                            return default;
                        }

                        marker = i;
                    }
                }

                var arg = marker.TypeArguments[0];

                var patternValues = new Dictionary<string, string?>(16) {
                    ["Namespace"] = option.ContainingNamespace.ToDisplayString(),
                    ["OptionName"] = option.Name,
                    ["OptionShort"] = option.MinimalName(),
                    ["OptionTypeofString"] = option.TypeArguments switch {
                        [ ]       => option.Name,
                        [ var t ] => $"{option.Name}<{{typeof({t.MinimalName()}).Name}}>",
                        _         => null
                    },
                    ["TypeArguments"] = option.TypeArguments switch {
                        [ { } t1 ] => $"<{t1.MinimalName()}>",
                        _          => null
                    },
                    ["OpenTypeArguments"] = null, // TODO remove. Option cannot be generic
                    ["SomeQualified"] = arg.GlobalName(),
                    ["SomeQualifiedForEquals"] = arg switch {
                        { IsReferenceType: true } => $"{arg.GlobalName()}?", // TODO check c# nullable support
                        _                         => arg.GlobalName()
                    },
                    ["OptionStateQualified"] = "global::Perf.Holders.OptionState",
                    ["SharedOptionQualified"] = "global::Perf.Holders.Option"
                };

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
