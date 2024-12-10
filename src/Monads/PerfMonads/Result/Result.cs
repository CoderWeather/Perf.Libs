namespace Perf.Monads.Result;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

/// <summary>
/// Source Generation and Reflection Marker
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IResultMonad<out TOk, out TError>
    where TOk : notnull
    where TError : notnull {
    TOk Ok { get; }
    TError Error { get; }
    bool IsOk { get; }
}

/// <summary>
/// Source Generation and Reflection Marker
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IResultMonad<out TOk, out TError, out TState> : IResultMonad<TOk, TError>
    where TOk : notnull
    where TError : notnull
    where TState : Enum {
    TState State { get; }
}

public enum ResultState : byte {
    Uninitialized = 0,
    Ok = 1,
    Error = 2
}

file sealed class ResultMonad_DebugView<TOk, TError> where TOk : notnull where TError : notnull {
    public ResultMonad_DebugView(Result<TOk, TError> result) {
        this.State = result.State;
        this.Value = this.State > 0 ? result.IsOk ? result.Ok : result.Error : "Uninitialized";
    }

    public ResultState State { get; }
    public object? Value { get; }
}

[DebuggerTypeProxy(typeof(ResultMonad_DebugView<,>))]
[DebuggerDisplay("{state > 0 ? (IsOk ? \"Ok: \" + ok.ToString() : $\"Error: \" + error.ToString()) : \"Uninitialized\"}")]
[JsonConverter(typeof(MonadResultJsonConverterFactory))]
[StructLayout(LayoutKind.Sequential)]
public readonly struct Result<TOk, TError> : IResultMonad<TOk, TError, ResultState>, IEquatable<Result<TOk, TError>>
    where TOk : notnull
    where TError : notnull {
    public Result() {
        state = ResultState.Uninitialized;
        ok = default!;
        error = default!;
    }

    public Result(TOk ok) {
        state = ResultState.Ok;
        this.ok = ok;
        error = default!;
    }

    public Result(TError error) {
        state = ResultState.Error;
        ok = default!;
        this.error = error;
    }

    public Result(Result.Ok<TOk> ok) : this(ok: ok.Value) { }
    public Result(Result.Error<TError> error) : this(error: error.Value) { }
    private readonly ResultState state;
    private readonly TOk ok;
    private readonly TError error;
    private static readonly string UninitializedException = $"Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Unitialized";
    private static readonly string ErrorAccessException = $"Cannot access Error. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Ok";
    private static readonly string OkAccessException = $"Cannot access Ok. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Error";
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TOk Ok =>
        state switch {
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            ResultState.Ok            => ok,
            ResultState.Error         => throw new InvalidOperationException(ErrorAccessException),
            _                         => throw new ArgumentOutOfRangeException()
        };
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TError Error =>
        state switch {
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            ResultState.Ok            => throw new InvalidOperationException(OkAccessException),
            ResultState.Error         => error,
            _                         => throw new ArgumentOutOfRangeException()
        };
    public bool IsOk => state is ResultState.Ok;
    public ResultState State => state;
    public static implicit operator Result<TOk, TError>(TOk ok) => new(ok);
    public static implicit operator Result<TOk, TError>(Result.Ok<TOk> ok) => new(ok.Value);
    public static implicit operator Result<TOk, TError>(TError error) => new(error);
    public static implicit operator Result<TOk, TError>(Result.Error<TError> error) => new(error.Value);

    public bool Equals(Result<TOk, TError> other) {
        if (state != other.state) {
            return false;
        }

        if (state is ResultState.Ok) {
            return EqualityComparer<TOk>.Default.Equals(ok, other.ok);
        }

        if (state is ResultState.Error) {
            return EqualityComparer<TError>.Default.Equals(error, other.error);
        }

        throw new InvalidOperationException();
    }

    public override bool Equals(object? obj) => obj is Result<TOk, TError> other && Equals(other);

    public override int GetHashCode() =>
        state switch {
            ResultState.Ok    => ok.GetHashCode(),
            ResultState.Error => error.GetHashCode(),
            _                 => 0
        };

    // Map
    public Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
        Func<TOk, TNewOk> mapOk,
        Func<TError, TNewError> mapError
    ) where TNewOk : notnull where TNewError : notnull {
        return IsOk ? mapOk(ok) : mapError(error);
    }

    public async ValueTask<Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
        Func<TOk, ValueTask<TNewOk>> mapOk,
        Func<TError, ValueTask<TNewError>> mapError
    ) where TNewOk : notnull where TNewError : notnull {
        return IsOk ? await mapOk(ok) : await mapError(error);
    }

    public Result<TNewOk, TError> Map<TNewOk>(Func<TOk, TNewOk> mapOk) where TNewOk : notnull => IsOk ? mapOk(ok) : error;

    public async ValueTask<Result<TNewOk, TError>> Map<TNewOk>(Func<TOk, ValueTask<TNewOk>> mapOk) where TNewOk : notnull =>
        IsOk ? await mapOk(ok) : error;

    public Result<TOk, TNewError> MapError<TNewError>(Func<TError, TNewError> mapError) where TNewError : notnull => IsOk ? ok : mapError(error);

    public async ValueTask<Result<TOk, TNewError>> MapError<TNewError>(Func<TError, ValueTask<TNewError>> mapError) where TNewError : notnull =>
        IsOk ? ok : await mapError(error);
}

public static class Result {
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Ok<T> : IEquatable<Ok<T>>
        where T : notnull {
        public Ok() {
            value = default!;
            init = false;
        }

        public Ok(T value) {
            this.value = value;
            init = true;
        }

        private readonly bool init;
        private readonly T value;
        private static readonly string UninitializedException = $"Ok<{typeof(T).Name}> is Unitialized";
        public T Value => init ? value : throw new InvalidOperationException(UninitializedException);
        public static implicit operator Ok<T>(T value) => new(value);
        public static implicit operator T(Ok<T> ok) => ok.Value;
        public bool Equals(Ok<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Ok<T> other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Error<T> : IEquatable<Error<T>>
        where T : notnull {
        public Error() {
            value = default!;
            init = false;
        }

        public Error(T value) {
            this.value = value;
            init = true;
        }

        private readonly bool init;
        private readonly T value;
        private static readonly string UninitializedException = $"Error<{typeof(T).Name}> is Unitialized";
        public T Value => init ? value : throw new InvalidOperationException(UninitializedException);
        public static implicit operator Error<T>(T value) => new(value);
        public static implicit operator T(Error<T> ok) => ok.Value;
        public bool Equals(Error<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Error<T> other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }
}

public static class GlobalMonadResultFunctions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<T> Ok<T>(T ok) where T : notnull => new(ok);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<Unit> Ok() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Error<T> Error<T>(T error) where T : notnull => new(error);
}
