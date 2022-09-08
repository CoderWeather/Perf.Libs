namespace Perf.ValueObjects.Generator;

internal record struct ValueObject(INamedTypeSymbol Type, ITypeSymbol KeyType, bool IsValidatable = false);

internal sealed class TypePack {
	public INamedTypeSymbol Symbol { get; }
	public List<BaseMemberPack> Members { get; } = new();
	public bool HaveConstructorWithKey { get; set; }
	public bool ImplementsValidatable { get; set; }
	public bool AddEqualityOperators { get; set; }
	public bool AddExtensionMethod { get; set; }

	public TypePack(INamedTypeSymbol type) {
		Symbol = type;
	}
}

internal abstract class BaseMemberPack {
	public ISymbol Symbol { get; }
	public ITypeSymbol OriginalType { get; }
	public ITypeSymbol Type { get; }
	public bool IsKey { get; set; }

	protected BaseMemberPack(ISymbol symbol, ITypeSymbol type) {
		Symbol = symbol;
		Type = type;
		OriginalType = Type.IsDefinition ? Type : Type.OriginalDefinition;
	}
}

internal sealed class FieldPack : BaseMemberPack {
	public new IFieldSymbol Symbol { get; }

	public FieldPack(IFieldSymbol fieldSymbol) :
		base(fieldSymbol, fieldSymbol.Type) {
		Symbol = fieldSymbol;
	}
}

internal sealed class PropertyPack : BaseMemberPack {
	public new IPropertySymbol Symbol { get; }

	public PropertyPack(IPropertySymbol propertySymbol) :
		base(propertySymbol, propertySymbol.Type) {
		Symbol = propertySymbol;
	}
}
