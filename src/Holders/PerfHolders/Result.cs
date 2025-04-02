// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Perf.Holders;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Exceptions;

file sealed class ResultHolder_DebugView<TOk, TError>
    where TOk : notnull
    where TError : notnull {
    public ResultHolder_DebugView(Result<TOk, TError> result) {
        State = (ResultState)typeof(Result<TOk, TError>)
            .GetField("state", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(result)!;
        Value = State switch {
            ResultState.Ok    => result.Ok,
            ResultState.Error => result.Error,
            _                 => "Default"
        };
    }

    // ReSharper disable once MemberCanBePrivate.Local
    public ResultState State { get; }
    public object Value { get; }
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
        state = ResultState.Default;
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TOk Ok =>
        state switch {
            ResultState.Ok    => ok,
            ResultState.Error => throw ResultHolderExceptions.WrongAccess<Result<TOk, TError>, TOk, TError>("Error", "Ok"),
            _                 => throw ResultHolderExceptions.OkObjectDefault<TOk>()
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public TError Error =>
        state switch {
            ResultState.Ok    => throw ResultHolderExceptions.WrongAccess<Result<TOk, TError>, TOk, TError>("Ok", "Error"),
            ResultState.Error => error,
            _                 => throw ResultHolderExceptions.Default<Result<TOk, TError>, TOk, TError>()
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsOk => state is ResultState.Ok;

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

    public TOther As<TOther>()
        where TOther : struct, IResultHolder<TOk, TError> =>
        ___HoldersInvisibleHelpers.CastResult<Result<TOk, TError>, TOk, TError, TOther>(in this);

    public override bool Equals(object? obj) =>
        obj switch {
            Result<TOk, TError> result => Equals(result),
            TOk o                      => Equals(o),
            Result.Ok<TOk> o           => Equals(o),
            TError er                  => Equals(er),
            Result.Error<TError> er    => Equals(er),
            _                          => false
        };

    public bool Equals(Result<TOk, TError> other) =>
        (state, other.state) switch {
            (ResultState.Ok, ResultState.Ok)           => EqualityComparer<TOk>.Default.Equals(ok, other.ok),
            (ResultState.Error, ResultState.Error)     => EqualityComparer<TError>.Default.Equals(error, other.error),
            (ResultState.Default, ResultState.Default) => true,
            _                                          => false
        };

    public bool Equals(TOk? v) => IsOk && EqualityComparer<TOk?>.Default.Equals(x: ok, y: v);
    public bool Equals(Result.Ok<TOk> v) => IsOk && EqualityComparer<TOk>.Default.Equals(x: ok, y: v.Value);
    public bool Equals(TError? er) => IsOk is false && EqualityComparer<TError?>.Default.Equals(x: error, y: er);
    public bool Equals(Result.Error<TError> er) => IsOk is false && EqualityComparer<TError>.Default.Equals(x: error, y: er.Value);

    public override int GetHashCode() =>
        state switch {
            ResultState.Ok    => ok.GetHashCode(),
            ResultState.Error => error.GetHashCode(),
            _                 => 0
        };

    public override string? ToString() =>
        state switch {
            ResultState.Ok    => ok.ToString(),
            ResultState.Error => error.ToString(),
            _                 => ""
        };

    string DebugPrint() =>
        state switch {
            ResultState.Ok    => $"Ok={ok}",
            ResultState.Error => $"Error={error}",
            _                 => "Default"
        };

    // Map
    public Result<TNewOk, TError> Map<TNewOk>(Func<TOk, TNewOk> mapOk)
        where TNewOk : notnull =>
        IsOk ? mapOk(ok) : error;

    public async ValueTask<Result<TNewOk, TError>> Map<TNewOk>(Func<TOk, ValueTask<TNewOk>> mapOk)
        where TNewOk : notnull =>
        IsOk ? await mapOk(ok).ConfigureAwait(false) : error;

    public Result<TOk, TNewError> MapError<TNewError>(Func<TError, TNewError> mapError)
        where TNewError : notnull =>
        IsOk ? ok : mapError(error);

    public async ValueTask<Result<TOk, TNewError>> MapError<TNewError>(Func<TError, ValueTask<TNewError>> mapError)
        where TNewError : notnull =>
        IsOk ? ok : await mapError(error).ConfigureAwait(false);

    public Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
        Func<TOk, TNewOk> mapOk,
        Func<TError, TNewError> mapError
    )
        where TNewOk : notnull
        where TNewError : notnull =>
        IsOk ? mapOk(ok) : mapError(error);

    public async ValueTask<Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
        Func<TOk, ValueTask<TNewOk>> mapOk,
        Func<TError, ValueTask<TNewError>> mapError
    )
        where TNewOk : notnull
        where TNewError : notnull =>
        IsOk ? await mapOk(ok).ConfigureAwait(false) : await mapError(error).ConfigureAwait(false);
}

public static class Result {
    enum ObjectState : byte {
        Default = 0,
        Value = 1
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct Ok<T> : IEquatable<Ok<T>>, IEquatable<T>
        where T : notnull {
        public Ok() {
            value = default!;
            state = ObjectState.Default;
        }

        public Ok(T value) {
            this.value = value;
            state = ObjectState.Value;
        }

        readonly ObjectState state;
        readonly T value;

        public T Value =>
            state switch {
                ObjectState.Value => value,
                _                 => throw ResultHolderExceptions.OkObjectDefault<T>()
            };

        public bool HaveValue => state is ObjectState.Value;

        public static implicit operator Ok<T>(T value) => new(value);
        public static explicit operator T(Ok<T> ok) => ok.Value;

        public bool Equals(Ok<T> other) =>
            (state, other.state) is (ObjectState.Value, ObjectState.Value) && EqualityComparer<T>.Default.Equals(value, other.value);

        public bool Equals(T? other) => HaveValue && EqualityComparer<T?>.Default.Equals(Value, other);

        public override bool Equals(object? obj) => obj is Ok<T> other && Equals(other) || obj is T t && Equals(t);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Ok<T> left, Ok<T> right) => left.Equals(right);
        public static bool operator !=(Ok<T> left, Ok<T> right) => left.Equals(right) is false;
        public override string ToString() => Value as string ?? value.ToString() ?? "";
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct Error<T> : IEquatable<Error<T>>, IEquatable<T>
        where T : notnull {
        public Error() {
            value = default!;
            state = ObjectState.Default;
        }

        public Error(T value) {
            this.value = value;
            state = ObjectState.Value;
        }

        readonly ObjectState state;
        readonly T value;

        public T Value =>
            state switch {
                ObjectState.Value => value,
                _                 => throw ResultHolderExceptions.ErrorObjectDefault<T>()
            };

        public bool HaveValue => state is ObjectState.Value;

        public static implicit operator Error<T>(T value) => new(value);
        public static explicit operator T(Error<T> ok) => ok.Value;

        public bool Equals(Error<T> other) =>
            (state, other.state) is (ObjectState.Value, ObjectState.Value) && EqualityComparer<T>.Default.Equals(value, other.value);

        public bool Equals(T? other) => HaveValue && EqualityComparer<T?>.Default.Equals(Value, other);

        public override bool Equals(object? obj) => obj is Error<T> other && Equals(other) || obj is T t && Equals(t);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Error<T> left, Error<T> right) => left.Equals(right);
        public static bool operator !=(Error<T> left, Error<T> right) => left.Equals(right) is false;
        public override string ToString() => Value as string ?? value.ToString() ?? "";
    }
}

public static class GlobalHolderResultFunctions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<T> Ok<T>(T ok)
        where T : notnull =>
        new(ok);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Ok<Unit> Ok() => Unit.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Error<T> Error<T>(T error)
        where T : notnull =>
        new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result.Error<Unit> Error() => Unit.Value;
}
