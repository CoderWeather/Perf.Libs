namespace Perf.ValueObjects;

public interface IValueObject<out TKey> {
	TKey Key { get; }
}

public interface IValidatableValueObject<out T> : IValueObject<T>, IValidatableValueObject { }

public interface IValidatableValueObject {
	bool IsValid();
}
