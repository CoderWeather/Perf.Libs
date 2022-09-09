namespace Perf.ValueObjects;

public class ValueObjectException<TValueObject> : Exception {
	public ValueObjectException(string message) : base(message) { }
	public ValueObjectException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class ValueObjectValidationException<TValueObject> : ValueObjectException<TValueObject> {
	public ValueObjectValidationException(TValueObject value) : base(
		$"{typeof(TValueObject).Name} is not valid with value: {value}"
	) {
		Value = value;
	}

	public TValueObject Value { get; }
}

public sealed class ValueObjectInitializationException<TValueObject> : ValueObjectException<TValueObject> {
	public ValueObjectInitializationException() : base(
		$"{typeof(TValueObject).Name} is not initialized"
	) { }
}

public sealed class ValueObjectEmptyException<TValueObject> : ValueObjectException<TValueObject> {
	public ValueObjectEmptyException() : base(
		$"{typeof(TValueObject).Name} is empty and value cannot be accessed"
	) { }
}

public static class ValueObjectException {
	public static ValueObjectValidationException<T> Validation<T>(T value) => new(value);
	public static ValueObjectInitializationException<T> Initialization<T>() => new();
	public static ValueObjectEmptyException<T> Empty<T>() => new();
}
