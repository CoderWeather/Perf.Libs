using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Utilities.Monads;

// Source Generation Marker
public interface IResultMonad<out TOk, out TError> where TOk : notnull where TError : notnull {
	TOk Ok { get; }
	TError Error { get; }
	bool IsOk { get; }
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TOk, TError> : IResultMonad<TOk, TError>, IEquatable<Result<TOk, TError>>
	where TOk : notnull
	where TError : notnull {
	public Result() {
		(ok, error, init, isOk) = (default!, default!, false, false);
	}

	public Result(TOk ok) {
		(this.ok, error, init, isOk) = (ok, default!, true, true);
	}

	public Result(Result.Ok<TOk> ok) {
		(this.ok, error, init, isOk) = (ok.Value, default!, true, true);
	}

	public Result(TError error) {
		(ok, this.error, init, isOk) = (default!, error, true, false);
	}

	public Result(Result.Error<TError> error) {
		(ok, this.error, init, isOk) = (default!, error.Value, true, false);
	}

	private readonly bool init;
	private readonly bool isOk;
	private readonly TOk ok;
	private readonly TError error;
	private static readonly string EmptyException = $"Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Empty";
	private static readonly string OkAccessException = $"Cannot access Ok. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Error";
	private static readonly string ErrorAccessException = $"Cannot access Error. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Ok";

	public TOk Ok => init ? isOk ? ok : throw new InvalidOperationException(OkAccessException) : throw new InvalidOperationException(EmptyException);

	public TError Error =>
		init ? !isOk ? error : throw new InvalidOperationException(ErrorAccessException) : throw new InvalidOperationException(EmptyException);

	public bool IsOk => init ? isOk : throw new InvalidOperationException(EmptyException);

	public static implicit operator Result<TOk, TError>(TOk ok) => new(ok);
	public static implicit operator Result<TOk, TError>(Result.Ok<TOk> ok) => new(ok.Value);
	public static implicit operator Result<TOk, TError>(TError error) => new(error);
	public static implicit operator Result<TOk, TError>(Result.Error<TError> error) => new(error.Value);

	public bool Equals(Result<TOk, TError> other) =>
		(IsOk && other.IsOk && EqualityComparer<TOk>.Default.Equals(ok, other.ok))
	 || (IsOk is false && other.IsOk is false && EqualityComparer<TError>.Default.Equals(error, other.error));

	public override bool Equals(object? obj) => obj is Result<TOk, TError> other && Equals(other);

	public override int GetHashCode() => IsOk ? ok.GetHashCode() : error.GetHashCode();
}

public static class Result {
	[StructLayout(LayoutKind.Auto)]
	public readonly struct Ok<T> : IEquatable<Ok<T>>
		where T : notnull {
		public Ok() {
			(value, init) = (default!, false);
		}

		public Ok(T value) {
			(this.value, init) = (value, true);
		}

		private readonly bool init;
		private readonly T value;
		private static readonly string EmptyException = $"Ok<{typeof(T).Name} is empty>";
		public T Value => init ? value : throw new InvalidOperationException(EmptyException);
		public static implicit operator Ok<T>(T value) => new(value);
		public static implicit operator T(Ok<T> ok) => ok.Value;
		public bool Equals(Ok<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
		public override bool Equals(object? obj) => obj is Ok<T> other && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();
	}

	[StructLayout(LayoutKind.Auto)]
	public readonly struct Error<T> : IEquatable<Error<T>>
		where T : notnull {
		public Error() {
			(value, init) = (default!, false);
		}

		public Error(T value) {
			(this.value, init) = (value, true);
		}

		private readonly bool init;
		private readonly T value;
		private static readonly string EmptyException = $"Error<{typeof(T).Name} is empty>";
		public T Value => init ? value : throw new InvalidOperationException(EmptyException);
		public static implicit operator Error<T>(T value) => new(value);
		public static implicit operator T(Error<T> ok) => ok.Value;
		public bool Equals(Error<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
		public override bool Equals(object? obj) => obj is Error<T> other && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();
	}
}

public static class ResultFunctions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Result.Ok<T> Ok<T>(T ok) where T : notnull => new(ok);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Result.Ok<Unit> Ok() => Unit.Value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Result.Error<T> Error<T>(T error) where T : notnull => new(error);
}
