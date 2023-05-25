namespace PerfXml.Generator;

sealed class ClassGenInfo {
    public readonly INamedTypeSymbol Symbol;
    public readonly List<BaseMemberGenInfo> XmlAttributes = new();
    public readonly List<BaseMemberGenInfo> XmlBodies = new();

    public string? ClassName;
    public bool InheritedClassName = false;
    public bool InheritedFromSerializable = false;

    public ClassGenInfo(INamedTypeSymbol symbol) {
        Symbol = symbol;
    }

    public string? AdditionalInheritanceMethodModifiers =>
        InheritedFromSerializable
            ? "override "
            : Symbol.IsSealed is false || Symbol.IsAbstract
                ? "virtual "
                : null;
}

abstract class BaseMemberGenInfo {
    public char? SplitChar;
    public bool TypeIsSerializable;

    public string? XmlName;

    protected BaseMemberGenInfo(ISymbol symbol, ITypeSymbol type) {
        Symbol = symbol;
        Type = type;
    }

    public ISymbol Symbol { get; }
    public ITypeSymbol Type { get; }

    public string TypeName =>
        Type switch {
            INamedTypeSymbol nts => nts.IsGenericType ? nts.ToString() : nts.Name,
            ITypeParameterSymbol => Type.Name,
            _                    => Type.ToString()
        };
}

sealed class FieldGenInfo : BaseMemberGenInfo {
    public FieldGenInfo(IFieldSymbol fieldSymbol) :
        base(fieldSymbol, fieldSymbol.Type) {
        Symbol = fieldSymbol;
    }

    public new IFieldSymbol Symbol { get; }
}

sealed class PropertyGenInfo : BaseMemberGenInfo {
    public PropertyGenInfo(IPropertySymbol propertySymbol) :
        base(propertySymbol, propertySymbol.Type) {
        Symbol = propertySymbol;
    }

    public new IPropertySymbol Symbol { get; }
}
