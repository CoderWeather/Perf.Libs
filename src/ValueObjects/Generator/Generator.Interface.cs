namespace Perf.ValueObjects.Generator;

public partial class ValueObjectGenerator {
	const string ValueObjectFromValidatableInterfacePattern = @"
partial {1} {2} {{
	public {2}() {{
		value = default;
		init = false;
	}}
	public {2}({3} value) {{
		this.value = value;
		init = true;
		if (IsValid() is false) throw ValueObjectException.Validation(this);
	}}
	[ValueObject.Key]
	private readonly {3} value;
	//public {3} Value => init ? value : throw ValueObjectException.Empty<{2}>();
	public {3} Value => init ? value : default({3});
	{3} IValueObject<{3}>.Key => Value;
	private readonly bool init;
	public static implicit operator {3}({2} vo) => vo.Value;
	public static explicit operator {2}({3} value) => new(value);
	{4}
	public override string ToString() => init ? Value.ToString() : """";
	public override int GetHashCode() => init ? Value.GetHashCode() : 0;
}}";

	static void WriteBodyFromInterfaceDefinition(IndentedTextWriter writer, ValueObject vo) {
        var keyType = vo.KeyType;
        var type = vo.Type;

        writer.WriteLineNoTabs(
	        string.Format(
		        ValueObjectFromValidatableInterfacePattern,
		        null,
		        type.IsRecord ? "record struct" : "struct",
		        type.MinimalName(),
		        keyType.MinimalName(),
		        vo.IsValidatable ? null : "private bool IsValid() => value != default;"
	        )
        );
    }
}
