namespace Perf.Holders.Attributes;

#if !NET9_0_OR_GREATER
/// <summary>
/// For older than net9 compatibility
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class HolderPropertyNameOverrideAttribute(Type type, string propertyName, string? isPropertyName = null) : Attribute {
    public Type Type => type;
    public string PropertyName => propertyName;
    public string? IsPropertyName => isPropertyName;
}
#endif
