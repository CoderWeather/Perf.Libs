namespace Perf.Holders.Generator;

using System.Text;
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

                var arg1 = marker.TypeArguments[0];
                var arg1Name = arg1.GlobalName();
                var arg2 = marker.TypeArguments[1];
                var arg2Name = arg2.GlobalName();

                if (arg1.Equals(arg2, SymbolEqualityComparer.Default)) {
                    return default;
                }

                var patternValues = new EquatableDictionary<string, string?> {
                    ["NamespaceDeclaration"] = result.ContainingNamespace.IsGlobalNamespace is false
                        ? $"namespace {result.ContainingNamespace.ToDisplayString()}\n"
                        : "",
                    ["ResultName"] = result.Name,
                    ["ResultShort"] = result.MinimalName(),
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
                        [ _, _ ] => "<,>",
                        [ _ ]    => "<>",
                        _        => null
                    },
                    ["IsOkProperty"] = "IsOk",
                    ["IsOkInterfaceProperty"] = "",
                    ["IsOkDeclarationModifiers"] = "",
                    ["OkProperty"] = "Ok",
                    ["OkField"] = "ok",
                    ["OkInterfaceProperty"] = "",
                    ["OkDeclarationModifiers"] = " ",
                    ["OkType"] = arg1Name,
                    ["OkTypeForEquals"] = arg1 switch {
                        ITypeParameterSymbol or { IsReferenceType: true } => $"{arg1Name}?",
                        _                                                 => arg1Name
                    },
                    ["ErrorProperty"] = "Error",
                    ["ErrorField"] = "error",
                    ["ErrorInterfaceProperty"] = "",
                    ["ErrorDeclarationModifiers"] = " ",
                    ["ErrorType"] = arg2Name,
                    ["ErrorTypeForEquals"] = arg2 switch {
                        ITypeParameterSymbol or { IsReferenceType: true } => $"{arg2Name}?",
                        _                                                 => arg2Name
                    },
                    ["ResultState"] = "global::Perf.Holders.ResultState",
                    ["BaseResult"] = "global::Perf.Holders.Result",
                    ["DebuggerBrowsableNever"] = "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                };

                var declaredPartialProperties = result.GetMembers().WhereOfType<IPropertySymbol>(x => x.IsPartialDefinition);
                if (declaredPartialProperties.Length > 0) {
                    var okSet = false;
                    var errorSet = false;
                    var isOkSet = false;

                    foreach (var ps in declaredPartialProperties) {
                        if (okSet is false && ps.Type.Equals(arg1, SymbolEqualityComparer.Default)) {
                            okSet = true;
                            patternValues["OkProperty"] = ps.Name;
                            patternValues["OkField"] = ps.Name.ToFieldFormat();
                            patternValues["OkDeclarationModifiers"] = "partial ";
                            if (ps.Name is not "Ok") {
                                patternValues["OkInterfaceProperty"] = $"""
                                        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
                                        {arg1Name} global::Perf.Holders.IResultHolder<{arg1Name}, {arg2Name}>.Ok => {ps.Name};
                                    """;
                            }
                        } else if (errorSet is false && ps.Type.Equals(arg2, SymbolEqualityComparer.Default)) {
                            errorSet = true;
                            patternValues["ErrorProperty"] = ps.Name;
                            patternValues["ErrorField"] = ps.Name.ToFieldFormat();
                            patternValues["ErrorDeclarationModifiers"] = "partial ";
                            if (ps.Name is not "Error") {
                                patternValues["ErrorInterfaceProperty"] = $"""
                                        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
                                        {arg2Name} global::Perf.Holders.IResultHolder<{arg1Name}, {arg2Name}>.Error => {ps.Name};
                                    """;
                            }
                        } else if (isOkSet is false && ps.Type is INamedTypeSymbol { SpecialType: SpecialType.System_Boolean }) {
                            isOkSet = true;
                            patternValues["IsOkProperty"] = ps.Name;
                            patternValues["IsOkDeclarationModifiers"] = "partial ";
                            if (ps.Name is not "IsOk") {
                                patternValues["IsOkInterfaceProperty"] = $"""
                                        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
                                        bool global::Perf.Holders.IResultHolder<{arg1Name}, {arg2Name}>.IsOk => {ps.Name};
                                    """;
                            }
                        }
                    }

                    if (isOkSet is false && okSet) {
                        patternValues["IsOkProperty"] = $"Is{patternValues["OkProperty"]}";
                        patternValues["IsOkInterfaceProperty"] = $"""
                                bool global::Perf.Holders.IResultHolder<{arg1Name}, {arg2Name}>.IsOk => Is{patternValues["OkProperty"]};
                            """;
                    }
                }

                return new BasicHolderContextInfo(
                    SourceFileName: SourceFileName(result),
                    PatternValues: patternValues
                );
            }
        );
        var filtered = types.Where(static x => x != default);

        var compInfo = context.CompilationProvider.SelectCompInfo();

        var typesAndCompInfo = filtered.Combine(compInfo);

        context.RegisterSourceOutput(
            typesAndCompInfo,
            static (context, tuple1) => {
                var ((minimalNameWithGenericMetadata, values), compInfo) = tuple1;

                values["DebugViewVisibility"] = compInfo.Version >= LanguageVersion.CSharp11
                    ? "file "
                    : "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n";
                values["NullableFileAnnotation"] = compInfo.Version >= LanguageVersion.CSharp8
                    ? "#nullable enable"
                    : "";

                var sourceText = PatternFormatter.FormatPattern(Patterns.Result, values);

                if (compInfo.OptimizationLevel is OptimizationLevel.Debug) {
                    context.AddSource($"{minimalNameWithGenericMetadata}.cs", SourceText.From(sourceText, Encoding.UTF8));
                } else if (compInfo.OptimizationLevel is OptimizationLevel.Release) {
                    context.AddSource($"{minimalNameWithGenericMetadata}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        );
    }

    static string SourceFileName(INamedTypeSymbol symbol) {
        return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
    }
}
