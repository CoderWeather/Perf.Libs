namespace Perf.Holders.Generator;

using Microsoft.CodeAnalysis;

static class SymbolExtensions {
    public static bool IsValueNullable(this ITypeSymbol type) =>
        type is INamedTypeSymbol {
            OriginalDefinition.Name: "Nullable", IsValueType: true, ContainingNamespace: { Name: "System" }
        };

    public static string GlobalName(this ITypeSymbol type) {
        var nullablePostfix = type.NullableAnnotation is NullableAnnotation.Annotated && type.IsValueNullable() is false
            ? "?"
            : null;
        return $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{nullablePostfix}";
    }

    static readonly SymbolDisplayFormat FullPathFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None
    );

    public static string FullPath(this ITypeSymbol type) => type.ToDisplayString(FullPathFormat);

    public static string MinimalName(this ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    static string MinimalName(this INamespaceSymbol ns, string ifResultEmpty = "Generated") {
        var assemblyName = ns.ContainingAssembly.Name;
        var result = ns.ToDisplayString().Replace($"{assemblyName}.", null);
        return result.Length > 0 ? result : ifResultEmpty;
    }
}
