namespace Perf.ValueObjects.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ValueObject : Attribute {
	public bool AddEqualityOperators { get; set; }
	public bool AddExtensionMethod { get; set; }

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class Key : Attribute { }
}
