// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberCanBeProtected.Global

namespace Perf.ValueObjects;

public abstract class ValueObjectException<TValueObject> : Exception {
    protected ValueObjectException(string message) : base(message) { }
    protected ValueObjectException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class ValueObjectValidationException<TValueObject>(TValueObject value)
    : ValueObjectException<TValueObject>($"{typeof(TValueObject).Name} is not valid with value: {value}") {
    public TValueObject Value { get; } = value;
}

public sealed class ValueObjectInitializationException<TValueObject>()
    : ValueObjectException<TValueObject>($"{typeof(TValueObject).Name} is not initialized and value cannot be accessed");

public sealed class ValueObjectEmptyException<TValueObject>()
    : ValueObjectException<TValueObject>($"{typeof(TValueObject).Name} is empty and value cannot be accessed");

public static class ValueObjectException {
    public static ValueObjectValidationException<T> Validation<T>(T value) => new(value);
    public static ValueObjectInitializationException<T> Initialization<T>() => new();
    public static ValueObjectEmptyException<T> Empty<T>() => new();
}
