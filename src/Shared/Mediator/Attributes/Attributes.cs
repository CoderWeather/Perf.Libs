namespace Mediator.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MediatorClassAttribute : Attribute {
	public MediatorClassAttribute(Type @class) {
		Class = @class;
	}

	public Type Class { get; }
	public bool StaticExecution { get; set; }
}

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ScopedMediatorClassAttribute : Attribute {
	public ScopedMediatorClassAttribute(Type @class) {
		Class = @class;
	}

	public Type Class { get; }
}
