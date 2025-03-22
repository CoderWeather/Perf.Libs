// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Perf.Holders;

using System.Diagnostics;
using System.Runtime.InteropServices;
using Exceptions;

file sealed class OptionHolder_DebugView<T>
    where T : notnull {
    public OptionHolder_DebugView(Option<T> option) {
        State = option.State;
        Value = State switch {
            OptionState.Some => option.Some,
            OptionState.None => "None",
            _                => "!!! Incorrect State !!!"
        };
    }

    // ReSharper disable once MemberCanBePrivate.Local
    public OptionState State { get; }
    public object? Value { get; }
}

[DebuggerTypeProxy(typeof(OptionHolder_DebugView<>))]
[DebuggerDisplay("{DebugPrint()}")]
[StructLayout(LayoutKind.Auto)]
public readonly struct Option<T> :
    IOptionHolder<T>,
    IEquatable<Option<T>>,
    IEquatable<T>,
    IEquatable<Option.Some<T>>
    where T : notnull {
    public Option() {
        state = OptionState.None;
        some = default!;
    }

    public Option(T some) {
        state = OptionState.Some;
        this.some = some;
    }

    public Option(Option.Some<T> some) {
        state = OptionState.Some;
        this.some = some.Value;
    }

    readonly OptionState state;
    readonly T some;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Some =>
        state switch {
            OptionState.Some => some,
            OptionState.None => throw OptionHolderExceptions.SomeAccessWhenNone<Option<T>, T>(),
            _                => throw OptionHolderExceptions.StateOutOfValidValues<Option<T>, T>(state)
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsSome =>
        state switch {
            OptionState.Some => true,
            OptionState.None => false,
            _                => throw OptionHolderExceptions.StateOutOfValidValues<Option<T>, T>(state)
        };

    public OptionState State => state;
    public static implicit operator Option<T>(T some) => new(some);
    public static implicit operator Option<T>(Option.Some<T> some) => new(some.Value);
    public static implicit operator Option<T>(Option.None _) => default;
    public static implicit operator bool(Option<T> option) => option.IsSome;
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
    public static bool operator !=(Option<T> left, Option<T> right) => left.Equals(right) is false;
    public static bool operator ==(Option<T> left, T right) => left.Equals(right);
    public static bool operator !=(Option<T> left, T right) => left.Equals(right) is false;
    public static bool operator ==(Option<T> left, Option.Some<T> right) => left.Equals(right);
    public static bool operator !=(Option<T> left, Option.Some<T> right) => left.Equals(right) is false;

    public TOther As<TOther>()
        where TOther : struct, IOptionHolder<T> =>
        ___HoldersInvisibleHelpers.CastOption<Option<T>, T, TOther>(in this);

    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    public bool Equals(Option<T> other) =>
        (state, other.state) switch {
            (OptionState.Some, OptionState.Some) => EqualityComparer<T>.Default.Equals(some, other.some),
            (OptionState.None, OptionState.None) => true,
            _                                    => throw OptionHolderExceptions.StateOutOfValidValues<Option<T>, T>(state)
        };

    public bool Equals(T? v) => IsSome && EqualityComparer<T?>.Default.Equals(some, v);
    public bool Equals(Option.Some<T> v) => IsSome && EqualityComparer<T>.Default.Equals(some, v.Value);

    public override int GetHashCode() {
        return state switch {
            OptionState.Some => some.GetHashCode(),
            OptionState.None => Option.None.Value.GetHashCode(),
            _                => throw OptionHolderExceptions.StateOutOfValidValues<Option<T>, T>(state)
        };
    }

    public override string? ToString() =>
        state switch {
            OptionState.Some => some.ToString(),
            OptionState.None => Option.None.Value.ToString(),
            _                => throw OptionHolderExceptions.StateOutOfValidValues<Option<T>, T>(state)
        };

    string DebugPrint() =>
        state switch {
            OptionState.Some => $"Some={some}",
            OptionState.None => "None",
            _                => "!!! Incorrect State !!!"
        };

    public Option<TNew> Map<TNew>(Func<T, TNew> map)
        where TNew : notnull =>
        IsSome ? map(some) : default(Option<TNew>);

    public async ValueTask<Option<TNew>> Map<TNew>(Func<T, ValueTask<TNew>> map)
        where TNew : notnull =>
        IsSome ? await map(some) : default(Option<TNew>);
}

public static class Option {
    enum ElState : byte {
        Uninitialized = 0,
        Initialized = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Some<T> : IEquatable<Some<T>> where T : notnull {
        public Some() {
            state = ElState.Uninitialized;
            value = default!;
        }

        public Some(T value) {
            state = ElState.Initialized;
            this.value = value;
        }

        readonly ElState state;
        readonly T value;

        public T Value =>
            state switch {
                ElState.Initialized   => value,
                ElState.Uninitialized => throw new InvalidOperationException($"Option.Some<{typeof(T).Name}> is Uninitialized"),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };

        public static implicit operator Some<T>(T value) => new(value);
        public static implicit operator T(Some<T> some) => some.Value;

        public bool Equals(Some<T> other) =>
            (state, other.state) switch {
                (ElState.Initialized, ElState.Initialized) => EqualityComparer<T>.Default.Equals(value, other.value),
                (ElState.Uninitialized, _) or (_, ElState.Uninitialized) =>
                    throw new InvalidOperationException($"Option.Some<{typeof(T).Name}> is Uninitialized"),
                _ => false
            };

        public override bool Equals(object? obj) => obj is Some<T> other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Some<T> left, Some<T> right) => left.Equals(right);
        public static bool operator !=(Some<T> left, Some<T> right) => left.Equals(right) is false;

        public override string? ToString() =>
            state switch {
                ElState.Initialized   => value as string ?? value?.ToString(),
                ElState.Uninitialized => throw new InvalidOperationException($"Option.Some<{typeof(T).Name}> is Uninitialized"),
                _                     => throw new ArgumentOutOfRangeException(nameof(state))
            };
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct None : IEquatable<None> {
        public static None Value => default;
        public override bool Equals(object? obj) => obj is None;
        public bool Equals(None other) => true;
        public override string ToString() => "()";
        public static implicit operator Unit(None _) => default;
        public static implicit operator None(Unit _) => default;
        public override int GetHashCode() => 0;
        public static bool operator ==(None left, None right) => left.Equals(right);
        public static bool operator !=(None left, None right) => left.Equals(right) is false;
    }
}

public static class GlobalHolderOptionFunctions {
    public static Option.Some<T> Some<T>(T v) => v;
    public static Option.Some<Unit> Some() => Unit.Value;
    public static Option.None None() => default;
}
