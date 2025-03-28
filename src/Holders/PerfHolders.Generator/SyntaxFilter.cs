namespace Perf.Holders.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class SyntaxFilter {
    public static bool IsResultHolder(this SyntaxNode sn) {
        if (sn is not StructDeclarationSyntax {
                Modifiers.Count: > 0,
                BaseList.Types: {
                    Count: > 0
                } blt,
                TypeParameterList: null or { Parameters.Count: <= 2 }
            } sds
            || sds.Modifiers.Any(SyntaxKind.PartialKeyword) is false
            || sds.Modifiers.Any(SyntaxKind.RefKeyword)
        ) {
            return false;
        }

        foreach (var bt in blt) {
            GenericNameSyntax? gns = null;
            if (bt is SimpleBaseTypeSyntax {
                    Type: QualifiedNameSyntax {
                        Right: GenericNameSyntax g1
                    }
                }
            ) {
                gns = g1;
            }

            if (bt is SimpleBaseTypeSyntax {
                    Type: GenericNameSyntax g2
                }
            ) {
                gns = g2;
            }

            if (gns is {
                    Identifier.Text: HolderTypeNames.ResultMarkerName,
                    TypeArgumentList.Arguments.Count: 2
                }
            ) {
                return true;
            }
        }

        return false;
    }

    public static bool IsOptionHolder(this SyntaxNode sn) {
        if (sn is not StructDeclarationSyntax {
                Modifiers.Count: > 0,
                BaseList.Types.Count: > 0
            } sds
            || sds.Modifiers.Any(SyntaxKind.PartialKeyword) is false
            || sds.Modifiers.Any(SyntaxKind.RefKeyword)
        ) {
            return false;
        }

        foreach (var bt in sds.BaseList.Types) {
            GenericNameSyntax? gns = null;
            if (bt is SimpleBaseTypeSyntax {
                    Type: QualifiedNameSyntax {
                        Right: GenericNameSyntax g1
                    }
                }
            ) {
                gns = g1;
            }

            if (bt is SimpleBaseTypeSyntax {
                    Type: GenericNameSyntax g2
                }
            ) {
                gns = g2;
            }

            if (gns is {
                    Identifier.Text: HolderTypeNames.OptionMarkerName,
                    TypeArgumentList.Arguments.Count: 1
                }
            ) {
                return true;
            }
        }

        return false;
    }
}
