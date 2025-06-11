namespace Perf.Holders.Attributes;

/// <summary>
/// For older than net9 compatibility
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class HolderPropertyNameOverrideAttribute(Type type, string propertyName, string? isPropertyName = null) : Attribute {
    public Type Type => type;
    public string PropertyName => propertyName;
    public string? IsPropertyName => isPropertyName;
}
