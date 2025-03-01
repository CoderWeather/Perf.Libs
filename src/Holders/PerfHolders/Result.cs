// ReSharper disable UnusedMember.Global

namespace Perf.Holders;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

file sealed class ResultHolder_DebugView<TOk, TError> where TOk : notnull where TError : notnull {
    public ResultHolder_DebugView(Result<TOk, TError> result) {
        State = result.State;
        Value = State switch {
            ResultState.Ok            => result.Ok,
            ResultState.Error         => result.Error,
            ResultState.Uninitialized => "Uninitialized",
            _                         => "!!!State is incorrect"
        };
    }

    // ReSharper disable once MemberCanBePrivate.Local
    public ResultState State { get; }
    public object? Value { get; }
}

[DebuggerTypeProxy(typeof(ResultHolder_DebugView<,>))]
[DebuggerDisplay("{DebugPrint()}")]
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TOk, TError> :
    IResultHolder<TOk, TError>,
    IEquatable<Result<TOk, TError>>,
    IEquatable<TOk>,
    IEquatable<Result.Ok<TOk>>
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
    readonly ResultState state;
    readonly TOk ok;
    readonly TError error;
    static readonly string UninitializedException = $"Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Unitialized";
    static readonly string ErrorAccessException = $"Cannot access Error. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Ok";
    static readonly string OkAccessException = $"Cannot access Ok. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Error";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TOk Ok =>
        state switch {
            ResultState.Ok            => ok,
            ResultState.Error         => throw new InvalidOperationException(OkAccessException),
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            _                         => throw new ArgumentOutOfRangeException(nameof(state))
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TError Error =>
        state switch {
            ResultState.Ok            => throw new InvalidOperationException(ErrorAccessException),
            ResultState.Error         => error,
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            _                         => throw new ArgumentOutOfRangeException(nameof(state))
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsOk =>
        state switch {
            ResultState.Ok            => true,
            ResultState.Error         => false,
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            _                         => throw new ArgumentOutOfRangeException(nameof(state))
        };

    public ResultState State => state;
    public static implicit operator Result<TOk, TError>(TOk ok) => new(ok);
    public static implicit operator Result<TOk, TError>(Result.Ok<TOk> ok) => new(ok.Value);
    public static implicit operator Result<TOk, TError>(TError error) => new(error);
    public static implicit operator Result<TOk, TError>(Result.Error<TError> error) => new(error.Value);
    public static implicit operator bool(Result<TOk, TError> result) => result.IsOk;
    public static bool operator ==(Result<TOk, TError> left, Result<TOk, TError> right) => left.Equals(right);
    public static bool operator !=(Result<TOk, TError> left, Result<TOk, TError> right) => left.Equals(right) is false;
    public static bool operator ==(Result<TOk, TError> left, TOk right) => left.Equals(right);
    public static bool operator !=(Result<TOk, TError> left, TOk right) => left.Equals(right) is false;
    public static bool operator ==(Result<TOk, TError> left, TError right) => left.Equals(right);
    public static bool operator !=(Result<TOk, TError> left, TError right) => left.Equals(right) is false;

    public TOther As<TOther>() where TOther : struct, IResultHolder<TOk, TError> {
        var t = this;
        return ___HoldersInvisibleHelpers.Cast<TOk, TError, TOther>(ref t);
    }

    public override bool Equals(object? obj) => obj is Result<TOk, TError> other && Equals(other);

    public bool Equals(Result<TOk, TError> other) =>
        (state, other.state) switch {
            (ResultState.Ok, ResultState.Ok)                                           => EqualityComparer<TOk>.Default.Equals(ok, other.ok),
            (ResultState.Error, ResultState.Error)                                     => EqualityComparer<TError>.Default.Equals(error, other.error),
            (ResultState.Ok, ResultState.Error) or (ResultState.Error, ResultState.Ok) => false,
            (ResultState.Uninitialized, _) or (_, ResultState.Uninitialized)           => throw new InvalidOperationException(UninitializedException),
            _                                                                          => throw new ArgumentOutOfRangeException(nameof(state))
        };

    public bool Equals(TOk? v) => IsOk && EqualityComparer<TOk?>.Default.Equals(x: ok, y: v);
    public bool Equals(Result.Ok<TOk> v) => IsOk && EqualityComparer<TOk>.Default.Equals(x: ok, y: v.Value);
    public bool Equals(TError? er) => IsOk is false && EqualityComparer<TError?>.Default.Equals(x: error, y: er);
    public bool Equals(Result.Error<TError> er) => IsOk is false && EqualityComparer<TError>.Default.Equals(x: error, y: er.Value);

    public override int GetHashCode() =>
        state switch {
            ResultState.Ok            => ok.GetHashCode(),
            ResultState.Error         => error.GetHashCode(),
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            _                         => throw new ArgumentOutOfRangeException(nameof(state))
        };

    public override string? ToString() =>
        state switch {
            ResultState.Ok            => ok.ToString(),
            ResultState.Error         => error.ToString(),
            ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
            _                         => throw new ArgumentOutOfRangeException(nameof(state))
        };

    string DebugPrint() =>
        state switch {
            ResultState.Ok            => $"Ok={ok}",
            ResultState.Error         => $"Error={error}",
            ResultState.Uninitialized => "Uninitialized",
            _                         => "!!! Incorrect State !!!"
        };

    // Map
    public Result<TNewOk, TError> Map<TNewOk>(Func<TOk, TNewOk> mapOk)
        where TNewOk : notnull =>
        IsOk ? mapOk(ok) : error;

    public async ValueTask<Result<TNewOk, TError>> Map<TNewOk>(Func<TOk, ValueTask<TNewOk>> mapOk)
        where TNewOk : notnull =>
        IsOk ? await mapOk(ok) : error;

    public Result<TOk, TNewError> MapError<TNewError>(Func<TError, TNewError> mapError)
        where TNewError : notnull =>
        IsOk ? ok : mapError(error);

    public async ValueTask<Result<TOk, TNewError>> MapError<TNewError>(Func<TError, ValueTask<TNewError>> mapError)
        where TNewError : notnull =>
        IsOk ? ok : await mapError(error);

    public Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
        Func<TOk, TNewOk> mapOk,
        Func<TError, TNewError> mapError
    ) where TNewOk : notnull where TNewError : notnull =>
        IsOk ? mapOk(ok) : mapError(error);

    public async ValueTask<Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
        Func<TOk, ValueTask<TNewOk>> mapOk,
        Func<TError, ValueTask<TNewError>> mapError
    ) where TNewOk : notnull where TNewError : notnull =>
        IsOk ? await mapOk(ok) : await mapError(error);
}

public static class Result {
    enum ElState : byte {
        Uninitialized = 0,
        Initialized = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Ok<T> : IEquatable<Ok<T>> {
        public Ok() {
            value = default!;
            state = ElState.Uninitialized;
        }

        public Ok(T value) {
            this.value = value;
            state = ElState.Initialized;
        }

        readonly ElState state;
        readonly T value;
        static readonly string UninitializedException = $"Result.Ok<{typeof(T).Name}> is Unitialized";

        public T Value =>
            state switch {
                ElState.Initialized   => value,
                ElState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public static implicit operator Ok<T>(T value) => new(value);
        public static implicit operator T(Ok<T> ok) => ok.Value;

        public override string? ToString() =>
            state switch {
                ElState.Initialized   => value?.ToString(),
                ElState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public bool Equals(Ok<T> other) =>
            (state, other.state) switch {
                (ElState.Initialized, ElState.Initialized)               => EqualityComparer<T>.Default.Equals(value, other.value),
                (ElState.Uninitialized, _) or (_, ElState.Uninitialized) => throw new InvalidOperationException(UninitializedException),
                _                                                        => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public override bool Equals(object? obj) => obj is Ok<T> other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? typeof(T).GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Error<T> : IEquatable<Error<T>> {
        public Error() {
            value = default!;
            state = ElState.Initialized;
        }

        public Error(T value) {
            this.value = value;
            state = ElState.Initialized;
        }

        readonly ElState state;
        readonly T value;
        static readonly string UninitializedException = $"Result.Error<{typeof(T).Name}> is Unitialized";

        public T Value =>
            state switch {
                ElState.Initialized   => value,
                ElState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public static implicit operator Error<T>(T value) => new(value);
        public static implicit operator T(Error<T> ok) => ok.Value;

        public override string? ToString() =>
            state switch {
                ElState.Initialized   => value?.ToString(),
                ElState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public bool Equals(Error<T> other) =>
            (state, other.state) switch {
                (ElState.Initialized, ElState.Initialized)               => EqualityComparer<T>.Default.Equals(value, other.value),
                (ElState.Uninitialized, _) or (_, ElState.Uninitialized) => throw new InvalidOperationException(UninitializedException),
                _                                                        => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public override bool Equals(object? obj) => obj is Error<T> other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? typeof(T).GetHashCode();
    }
}

public static class GlobalHolderResultFunctions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<T> Ok<T>(T ok) where T : notnull => new(ok);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<Unit> Ok() => Unit.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Error<T> Error<T>(T error) where T : notnull => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Error<Unit> Error() => Unit.Value;
}
