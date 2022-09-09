namespace Perf.SourceGeneration.Internal;

internal static class SyntaxExtensions {
    public static bool IsPartial(this SyntaxNode node) {
        return node switch {
            TypeDeclarationSyntax t => t.Modifiers.Any(SyntaxKind.PartialKeyword),
            _                       => false
        };
    }

    public static bool All<T>(this SeparatedSyntaxList<T> list, Func<T, bool> predicate)
        where T : SyntaxNode {
        foreach (var t in list) {
            if (predicate.Invoke(t) is false) {
                return false;
            }
        }

        return true;
    }

    public static bool AllBy<T>(this SeparatedSyntaxList<T> list, Func<T, bool> filter, Func<T, bool> predicate)
        where T : SyntaxNode {
        foreach (var t in list) {
            if (filter.Invoke(t) is false) {
                continue;
            }

            if (predicate.Invoke(t) is false) {
                return false;
            }
        }

        return true;
    }

    public static bool Any<T>(this SeparatedSyntaxList<T> list, Func<T, bool> predicate)
        where T : SyntaxNode {
        foreach (var t in list) {
            if (predicate.Invoke(t)) {
                return true;
            }
        }

        return false;
    }

    public static bool AnyBy<T>(this SeparatedSyntaxList<T> list, Func<T, bool> filter, Func<T, bool> predicate)
        where T : SyntaxNode {
        foreach (var t in list) {
            if (filter.Invoke(t) is false) {
                continue;
            }

            if (predicate.Invoke(t)) {
                return true;
            }
        }

        return false;
    }
}