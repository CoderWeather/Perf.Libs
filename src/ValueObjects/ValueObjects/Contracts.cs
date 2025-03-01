// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.ValueObjects;

public interface IValueObject<out T> {
    T Value { get; }
}

public interface IValidatableValueObject<T> : IValueObject<T> {
    bool Validate(T valueToValidate);
}
