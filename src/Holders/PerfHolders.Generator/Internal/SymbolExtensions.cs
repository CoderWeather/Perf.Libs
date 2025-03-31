// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Global

namespace Perf.Holders.Generator.Internal;

using Microsoft.CodeAnalysis;

static class SymbolExtensions {
    static readonly SymbolDisplayFormat GlobalFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

    public static string GlobalName(this ITypeSymbol type) => type.ToDisplayString(GlobalFormat);

    static readonly SymbolDisplayFormat FullPathFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None
    );

    public static string FullPath(this ITypeSymbol type) => type.ToDisplayString(FullPathFormat);

    public static string MinimalName(this ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static ITypeSymbol MakeNullable(this ITypeSymbol ts, Compilation compilation) {
        if (ts.IsReferenceType) {
            return ts.NullableAnnotation is NullableAnnotation.Annotated
                ? ts
                : ts.WithNullableAnnotation(NullableAnnotation.Annotated);
        }

        if (ts.MetadataName is "Nullable`1") {
            return ts;
        }

        var nullableStruct = compilation.GetSpecialType(SpecialType.System_Nullable_T);
        return nullableStruct.Construct(ts);
    }

    public static string? GetNamespaceString(this INamedTypeSymbol ts) =>
        ts.ContainingNamespace.IsGlobalNamespace
            ? null
            : ts.ContainingNamespace.ToDisplayString();
}
