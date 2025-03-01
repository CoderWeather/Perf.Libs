namespace Utilities.SourceGeneration;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RecordAsDtoAttribute : Attribute {
    public RecordAsDtoAttribute() { }

    public RecordAsDtoAttribute(string accessibility) {
        Accessibility = accessibility;
    }

    public string? Accessibility { get; }
    public bool Struct { get; set; }
    public string NamePostfix { get; set; } = "Dto";
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AddAttributeToGeneratedDto : Attribute {
    public AddAttributeToGeneratedDto(Type attributeType, params object?[] parameters) {
        AttributeType = attributeType;
        Parameters = parameters;
    }

    public Type AttributeType { get; }
    public object?[] Parameters { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class AddInterfacesToGeneratedDto : Attribute {
    public AddInterfacesToGeneratedDto(params Type[] interfaces) {
        Interfaces = interfaces;
    }

    public Type[] Interfaces { get; }
}

public interface IGeneratedDto { }

public interface IGeneratedDto<in TOrigin> : IGeneratedDto {
    void Clear();
    void ApplyValues(TOrigin origin);
}

public interface IHaveGeneratedDto<TDto>
    where TDto : IGeneratedDto, new() { }
