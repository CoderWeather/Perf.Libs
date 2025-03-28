namespace Perf.Holders.Generator.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
sealed class InvalidHolderDeclarationAnalyzer : DiagnosticAnalyzer {
    public static readonly DiagnosticDescriptor OnlyStructRule = new(
        id: "PRFH001",
        title: "Invalid holder type declaration",
        messageFormat: "Generated holder type must be declared as struct",
        category: "Declaration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Generated holder type cannot be class, record struct or ref struct."
    );

    public static readonly DiagnosticDescriptor OnlyOneInterfaceMarkerRule = new(
        id: "PRFH002",
        title: "Invalid holder type interface declaration",
        messageFormat: "Generated holder type must implement only one of IResultHolder or IOptionHolder",
        category: "Declaration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "For generated holder type you can choose only one type of holder."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ OnlyStructRule, OnlyOneInterfaceMarkerRule ];

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.StructDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration
        );
    }

    enum HolderType { Undefined = 0, Result = 1, Option = 2 }

    static void Analyze(SyntaxNodeAnalysisContext context) {
        var ct = context.CancellationToken;
        var node = (TypeDeclarationSyntax)context.Node;

        if (node is not {
                BaseList.Types: {
                    Count: >= 1
                } blt
            }
        ) {
            return;
        }

        // symbol -> GetMembers() -> Where PropertySymbol -> IsPartialDefinition
        // IOptionHolder<Unit> restrict to declare

        var foundMarkerInSyntax = false;
        foreach (var sbt in blt) {
            foundMarkerInSyntax = sbt switch {
                SimpleBaseTypeSyntax {
                    Type: QualifiedNameSyntax {
                        Right: GenericNameSyntax {
                            Identifier.Text : "IResultHolder" or "IOptionHolder", TypeArgumentList.Arguments.Count: 2
                        }
                    }
                } or SimpleBaseTypeSyntax {
                    Type: GenericNameSyntax {
                        Identifier.Text : "IResultHolder" or "IOptionHolder", TypeArgumentList.Arguments.Count: 2
                    }
                } => true,
                _ => foundMarkerInSyntax
            };
        }

        if (foundMarkerInSyntax is false) {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(node, ct) is not { } type) {
            return;
        }

        var holderType = HolderType.Undefined;
        INamedTypeSymbol? marker = null;
        foreach (var i in type.Interfaces) {
            var fullInterfaceName = i.FullPath();
            switch (fullInterfaceName) {
                case { } s when holderType > HolderType.Undefined && s.StartsWith("Perf.Holders."):
                    context.ReportDiagnostic(Diagnostic.Create(OnlyOneInterfaceMarkerRule, node.GetLocation()));
                    break;
                case HolderTypeNames.ResultMarkerFullName: {
                    marker = i;
                    holderType = HolderType.Result;
                    continue;
                }
                case HolderTypeNames.OptionMarkerFullName: {
                    marker = i;
                    holderType = HolderType.Option;
                    continue;
                }
            }
        }

        if (marker is null) {
            return;
        }

        _ = holderType;
    }
}
