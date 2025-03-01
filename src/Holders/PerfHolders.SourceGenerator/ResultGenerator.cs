namespace Perf.Holders.Generator;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class ResultHolderGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => {
                    if (node is not StructDeclarationSyntax {
                            BaseList.Types.Count: > 0
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
                                        Identifier.Text : "IResultHolder",
                                        TypeArgumentList.Arguments.Count: 2
                                    }
                                }
                            }:
                            case SimpleBaseTypeSyntax {
                                Type: GenericNameSyntax {
                                    Identifier.Text : "IResultHolder",
                                    TypeArgumentList.Arguments.Count: 2
                                }
                            }:
                                return true;
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
                        if (i.FullPath() is not Constants.ResultInterfaceFullName) {
                            continue;
                        }

                        if (marker != null) {
                            return default;
                        }

                        marker = i;
                    }

                    var arg1 = marker.TypeArguments[0];
                    var arg2 = marker.TypeArguments[1];

                    var patternValues = new Dictionary<string, string?> {
                        ["Namespace"] = result.ContainingNamespace.ToDisplayString(),
                        ["ResultName"] = result.Name,
                        ["ResultShort"] = result.MinimalName(),
                        ["ResultTypeofString"] = result.TypeArguments switch {
                            [ ]                => result.Name,
                            [ var t1 ]         => $"{result.Name}<{{typeof({t1.MinimalName()}).Name}}>",
                            [ var t1, var t2 ] => $"{result.Name}<{{typeof({t1.MinimalName()}).Name}}, {{typeof({t2.MinimalName()}).Name}}>",
                            _                  => result.MinimalName()
                        },
                        ["TypeArguments"] = result.TypeArguments switch {
                            [ ITypeParameterSymbol t1 ]                          => $"<{t1.Name}>",
                            [ var t1 ]                                           => $"<{t1.MinimalName()}>",
                            [ ITypeParameterSymbol t1, ITypeParameterSymbol t2 ] => $"<{t1.Name}, {t2.Name}>",
                            [ ITypeParameterSymbol t1, var t2 ]                  => $"<{t1.Name}, {t2.MinimalName()}>",
                            [ var t1, ITypeParameterSymbol t2 ]                  => $"<{t1.MinimalName()}, {t2.Name}>",
                            [ var t1, var t2 ]                                   => $"<{t1.MinimalName()}, {t2.MinimalName()}>",
                            _                                                    => null
                        },
                        ["OpenTypeArguments"] = result.TypeArguments switch {
                            [ _ ]    => "<>",
                            [ _, _ ] => "<,>",
                            _        => null
                        },
                        ["OkQualified"] = arg1 is ITypeParameterSymbol ? arg1.Name : arg1.GlobalName(),
                        ["OkQualifiedForEquals"] = arg1 switch {
                            ITypeParameterSymbol      => arg1.Name,
                            { IsReferenceType: true } => $"{arg1.GlobalName()}?",
                            _                         => arg1.GlobalName()
                        },
                        ["ErrorQualified"] = arg2 is ITypeParameterSymbol ? arg2.Name : arg2.GlobalName(),
                        ["ErrorQualifiedForEquals"] = arg2 switch {
                            ITypeParameterSymbol      => arg2.Name,
                            { IsReferenceType: true } => $"{arg2.GlobalName()}?",
                            _                         => arg2.GlobalName()
                        },
                        ["ResultStateQualified"] = "global::Perf.Holders.ResultState",
                        ["SharedResultQualified"] = "global::Perf.Holders.Result"
                    };

                    return new BasicHolderContextInfo(
                        MinimalNameWithGenericMetadata: MinimalNameWithGenericMetadata(result),
                        PatternValues: patternValues
                    );
                }
            )
            .WithTrackingName("Initial syntax selection and semantic transforming");
        var filtered = types
            .Where(static x => x != default)
            .WithTrackingName("Filtered after transforming");

        var compInfo = context
            .CompilationProvider
            .Select(static (c, _) => {
                    LanguageVersion? langVersion = c is CSharpCompilation comp ? comp.LanguageVersion : null;
                    return new CompInfo(langVersion);
                }
            )
            .WithTrackingName("Getting LangVersion from CompilationProvider");

        var typesAndCompInfo = filtered
            .Combine(compInfo)
            .WithTrackingName("Combining filtered entries with compilation info");

        context.RegisterSourceOutput(
            typesAndCompInfo,
            static (context, tuple1) => {
                var (tuple2, compInfo) = tuple1;
                var values = tuple2.PatternValues;

                values["DebugViewVisibility"] = compInfo.Version is >= LanguageVersion.CSharp11
                    ? "file "
                    : "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n";

                var sourceText = PatternFormatter.Format(
                    Patterns.Result2,
                    values
                );

                context.AddSource($"{tuple2.MinimalNameWithGenericMetadata}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        );
    }

    static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
