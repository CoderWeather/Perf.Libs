namespace Utilities.SourceGeneration;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CopyTypeMembersAttribute : Attribute {
	public CopyTypeMembersAttribute(Type origin) => Origin = origin;
	public CopyTypeMembersAttribute(string fullName) => Origin = null;
	public Type? Origin { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreMembersAttribute : Attribute {
	public IgnoreMembersAttribute(params string[] members) {
		Members = members;
	}

	public string[] Members { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class IncludeMembersAttribute : Attribute {
	public IncludeMembersAttribute(params string[] members) {
		Members = members;
	}

	public string[] Members { get; }
}
