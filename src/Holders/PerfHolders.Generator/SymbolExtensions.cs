// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Global

namespace Perf.Holders.Generator;

using Microsoft.CodeAnalysis;

static class SymbolExtensions {
    public static string GlobalName(this ITypeSymbol type) {
        return type.ToDisplayString(GlobalFormat);
    }

    static readonly SymbolDisplayFormat GlobalFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

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
