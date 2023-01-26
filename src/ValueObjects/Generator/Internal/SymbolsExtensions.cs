namespace Perf.ValueObjects.Generator.Internal;

enum DeclarationKind {
    Unrecognized,
    Record,
    Class,
    RecordStruct,
    Struct,
    Interface
}

static class SymbolsExtensions {
#region Base Classes

    public static bool HasBaseClass(this ITypeSymbol type) => type.BaseType?.Name is not "Object";

#endregion

    public static string MinimalName(this INamespaceSymbol ns, string ifResultEmpty = "Generated") {
        var assemblyName = ns.ContainingAssembly.Name;
        var result = ns.ToDisplayString().Replace($"{assemblyName}.", null);
        return result.Length > 0 ? result : ifResultEmpty;
    }

#region Types

    public static bool IsPartial(this INamedTypeSymbol ts) {
        if (ts.DeclaringSyntaxReferences.IsDefaultOrEmpty) {
            return false;
        }

        foreach (var sr in ts.DeclaringSyntaxReferences) {
            switch (sr.GetSyntax()) {
            case ClassDeclarationSyntax c when c.Identifier.Text == ts.Name:     return c.Modifiers.Any(SyntaxKind.PartialKeyword);
            case RecordDeclarationSyntax r when r.Identifier.Text == ts.Name:    return r.Modifiers.Any(SyntaxKind.PartialKeyword);
            case InterfaceDeclarationSyntax i when i.Identifier.Text == ts.Name: return i.Modifiers.Any(SyntaxKind.PartialKeyword);
            }
        }

        return false;
    }

    public static string Accessibility(this ITypeSymbol type) =>
        type.DeclaredAccessibility switch {
            Microsoft.CodeAnalysis.Accessibility.Public               => "public",
            Microsoft.CodeAnalysis.Accessibility.Internal             => "internal",
            Microsoft.CodeAnalysis.Accessibility.Protected            => "protected",
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => "protected internal",
            Microsoft.CodeAnalysis.Accessibility.Private              => "private",
            _                                                         => throw new ArgumentOutOfRangeException(nameof(type.DeclaredAccessibility))
        };

    public static string DeclarationString(this ITypeSymbol ts) =>
        ts is INamedTypeSymbol
            ? ts switch {
                { IsRecord: true, IsReferenceType: true } => "record",
                { IsReferenceType: true }                 => "class",
                { IsRecord: true, IsValueType: true }     => "record struct",
                { IsValueType: true }                     => "struct",
                { TypeKind: TypeKind.Interface }          => "interface",
                _                                         => throw new ArgumentOutOfRangeException(nameof(ts))
            }
            : throw new();

    public static DeclarationKind DeclarationType(this ITypeSymbol ts) =>
        ts switch {
            { IsRecord: true, IsReferenceType: true } => DeclarationKind.Record,
            { IsReferenceType: true }                 => DeclarationKind.Class,
            { IsRecord: true, IsValueType: true }     => DeclarationKind.RecordStruct,
            { IsValueType: true }                     => DeclarationKind.Struct,
            { TypeKind: TypeKind.Interface }          => DeclarationKind.Interface,
            _                                         => DeclarationKind.Unrecognized
        };

    public static bool StrictEquals<T>(this T s, T other) where T : ISymbol => s.Equals(other, SymbolEqualityComparer.Default);

    public static bool StrictEqualsNullable<T>(this T ts, T other) where T : ISymbol => ts.Equals(other, SymbolEqualityComparer.IncludeNullability);

    public static bool IsPrimitive(this ITypeSymbol type) =>
        type.IsValueType
     || type.IsValueNullable()
     || type.IsEnum()
     || type.Name is "String";

    public static bool IsString(this ITypeSymbol type) => type.Name is "String";

    public static bool IsList(this ITypeSymbol type) => type.Name is "List";

    public static bool IsValueNullable(this ITypeSymbol type) =>
        type is INamedTypeSymbol {
            OriginalDefinition.Name: "Nullable", IsValueType: true
        };

    public static bool IsEnum(this ITypeSymbol type) => type.IsValueType && type.TypeKind is TypeKind.Enum;

    public static ITypeSymbol? IfValueNullableGetInnerType(this ITypeSymbol type) =>
        type.IsValueNullable() && type is INamedTypeSymbol nt
            ? nt.TypeArguments[0]
            : null;

    public static string? AsString(this TypedConstant tc) => tc.Value as string;

    public static T? As<T>(this TypedConstant tc) => tc.Value is T v ? v : default;

#endregion

#region Attributes

    public static bool HasAttribute(this ISymbol? type, INamedTypeSymbol? attributeType) {
        return attributeType is not null
         && type?.GetAttributes()
               .Any(x => x.AttributeClass?.StrictEquals(attributeType) is true) is true;
    }

    public static bool HasAttribute(this ISymbol type, string typeFullPath) {
        return type.GetAttributes().Any(x => x.AttributeClass?.FullPath().Equals(typeFullPath) is true);
    }

    public static AttributeData? TryGetAttribute(this ISymbol type, INamedTypeSymbol? attributeType) {
        return attributeType is not null
            ? type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.StrictEquals(attributeType) ?? false)
            : null;
    }

    public static AttributeData? TryGetAttribute(this ISymbol symbol, string typeFullPath) {
        return symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.FullPath().Equals(typeFullPath) is true);
    }

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string typeFullPath) {
        return symbol.GetAttributes().Where(x => x.AttributeClass?.FullPath().Equals(typeFullPath) is true);
    }

    public static AttributeData GetAttribute(this ISymbol type, INamedTypeSymbol attribute) =>
        type.TryGetAttribute(attribute) ?? throw new($"{attribute} attribute not found for {type}");

    public static AttributeData GetAttribute(this ISymbol symbol, string typeFullPath) =>
        symbol.TryGetAttribute(typeFullPath) ?? throw new($"{typeFullPath} attribute not found for {symbol}");

#endregion

#region Ancestors

    public static IEnumerable<INamedTypeSymbol> GetAllAncestors(this ITypeSymbol type) {
        if (type.IsPrimitive()) {
            yield break;
        }

        var baseType = type.BaseType;

        while (baseType is not null && baseType.Name is not "object") {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    public static INamedTypeSymbol? FindOldestAncestor(this ITypeSymbol type, Func<ITypeSymbol?, bool> predicate) =>
        type.GetAllAncestors().LastOrDefault(predicate.Invoke);

#endregion
}
