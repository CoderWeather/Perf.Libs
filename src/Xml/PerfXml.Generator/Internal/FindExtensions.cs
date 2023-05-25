namespace PerfXml.Generator.Internal;

static class FindExtensions {
    public static IMethodSymbol? FindRecordTypeConstructor(this INamedTypeSymbol symbol) {
        if (symbol.IsRecord is false) {
            return null;
        }

        var constructors = symbol.InstanceConstructors;

        if (constructors.Length is not 2) {
            return null;
        }

        var c1 = constructors[0];
        var c2 = constructors[1];

        if (c1.Parameters.Length is 1 && c1.Parameters[0].Equals(symbol, SymbolEqualityComparer.Default)) {
            return c2;
        }

        return c1;
    }

    public static IMethodSymbol? FindRecordTypeConstructor(this INamedTypeSymbol symbol, RecordDeclarationSyntax syntax) {
        if (symbol.IsRecord is false) {
            return null;
        }

        var syntaxMainConstructorParameters = syntax.ParameterList?.Parameters;
        var symbolConstructors = symbol.InstanceConstructors;

        if (syntaxMainConstructorParameters.HasValue is false) {
            return symbolConstructors.First(x => x.Parameters.Length is 0);
        }

        var syntaxParameters = syntaxMainConstructorParameters.Value;

        foreach (var c in symbolConstructors) {
            var parameters = c.Parameters;

            if (syntaxParameters.Count is 1 && parameters.Length is 1) {
                var t1 = syntaxParameters[0];
                var t2 = parameters[0];
                if (t1.Identifier.Text == t2.Name && t1.Type?.ToString() == t2.Type.ToString()) {
                    return c;
                }
            }

            if (parameters.Length == syntaxParameters.Count) {
                var e1 = parameters.GetEnumerator();
                var e2 = syntaxParameters.GetEnumerator();

                while (e1.MoveNext() && e2.MoveNext()) {
                    var t1 = e2.Current;
                    var t2 = e1.Current;
                    if (t1.Identifier.Text != t2.Name || t1.Type?.ToString() != t2.Type.ToString()) {
                        break;
                    }
                }

                if (e1.MoveNext() is false && e2.MoveNext() is false) {
                    return c;
                }
            }
        }

        return null;
    }
}
