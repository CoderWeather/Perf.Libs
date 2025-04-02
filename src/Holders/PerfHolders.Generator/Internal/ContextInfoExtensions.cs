namespace Perf.Holders.Generator.Internal;

using Microsoft.CodeAnalysis;
using Types;

static class ContextInfoExtensions {
    public static EquatableList<HolderContainingType> GetContainingTypeList(this INamedTypeSymbol nts) {
        if (nts.ContainingType is null) {
            return default;
        }

        EquatableList<HolderContainingType> types = [ ];
        var t = nts;
        do {
            t = t.ContainingType;

            var accessibility = t.DeclaredAccessibility.MapToTypeAccessibility();

            HolderContainingType containingType = t switch {
                { IsReferenceType: true, IsRecord: true } => new(
                    Accessibility: accessibility,
                    Kind: "record",
                    Name: t.Name
                ),
                { IsReferenceType: true } => new(
                    Accessibility: accessibility,
                    Kind: "class",
                    Name: t.Name
                ),
                { IsValueType: true, IsRecord: true } => new(
                    Accessibility: accessibility,
                    Kind: "record struct",
                    Name: t.Name
                ),
                { IsValueType: true } => new(
                    Accessibility: accessibility,
                    Kind: "struct",
                    Name: t.Name
                ),
                _ => default
            };

            if (containingType != default) {
                types.Add(containingType);
            }
        } while (t.ContainingType != null);

        return types;
    }

    public static TypeAccessibility MapToTypeAccessibility(this Accessibility accessibility) =>
        accessibility switch {
            Accessibility.Public   => TypeAccessibility.Public,
            Accessibility.Private  => TypeAccessibility.Private,
            Accessibility.Internal => TypeAccessibility.Internal,
            _                      => TypeAccessibility.None
        };
}
