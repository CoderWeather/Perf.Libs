namespace Perf.ValueObjects.Generator;

record struct ValueObject(INamedTypeSymbol Type, ITypeSymbol KeyType, bool IsValidatable = false);

sealed class TypePack {
    public TypePack(INamedTypeSymbol type) {
        Symbol = type;
    }

    public INamedTypeSymbol Symbol { get; }
    public List<BaseMemberPack> Members { get; } = new();
    public bool HaveConstructorWithKey { get; set; }
    public bool ImplementsValidatable { get; set; }
    public bool AddEqualityOperators { get; set; }
    public bool AddExtensionMethod { get; set; }
}

abstract class BaseMemberPack {
    protected BaseMemberPack(ISymbol symbol, ITypeSymbol type) {
        Symbol = symbol;
        Type = type;
        OriginalType = Type.IsDefinition ? Type : Type.OriginalDefinition;
    }

    public ISymbol Symbol { get; }
    public ITypeSymbol OriginalType { get; }
    public ITypeSymbol Type { get; }
    public bool IsKey { get; set; }
}

sealed class FieldPack : BaseMemberPack {
    public FieldPack(IFieldSymbol fieldSymbol) :
        base(fieldSymbol, fieldSymbol.Type) {
        Symbol = fieldSymbol;
    }

    public new IFieldSymbol Symbol { get; }
}

sealed class PropertyPack : BaseMemberPack {
    public PropertyPack(IPropertySymbol propertySymbol) :
        base(propertySymbol, propertySymbol.Type) {
        Symbol = propertySymbol;
    }

    public new IPropertySymbol Symbol { get; }
}
