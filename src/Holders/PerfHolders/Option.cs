// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToAutoPropertyWhenPossible

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

    public Option(T? some) {
        if (some is not null) {
            state = OptionState.Some;
            this.some = some;
        } else {
            state = OptionState.None;
            this.some = default!;
        }
    }

    public Option(Option.Some<T?> someObject) : this(someObject.Value) { }

    readonly OptionState state;
    readonly T some;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Some =>
        state switch {
            OptionState.Some => some,
            OptionState.None => throw OptionHolderExceptions.SomeAccessWhenNone<Option<T>, T>("Some"),
            _                => throw OptionHolderExceptions.Default<Option<T>, T>()
        };

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsSome => state is OptionState.Some;

    public OptionState State => state;
    public static implicit operator Option<T>(T? some) => new(some);
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

    public override bool Equals(object? obj) =>
        obj switch {
            null             => false,
            Option<T> o      => Equals(o),
            Option.Some<T> s => Equals(s),
            T v              => Equals(v),
            Option.None      => IsSome is false,
            _                => false
        };

    public bool Equals(Option<T> other) =>
        (state, other.state) switch {
            (OptionState.Some, OptionState.Some) => EqualityComparer<T>.Default.Equals(some, other.some),
            (OptionState.None, OptionState.None) => true,
            _                                    => false
        };

    public bool Equals(T? v) => IsSome && EqualityComparer<T?>.Default.Equals(some, v);
    public bool Equals(Option.Some<T> v) => IsSome && EqualityComparer<T?>.Default.Equals(some, v.Value);

    public override int GetHashCode() {
        return state switch {
            OptionState.Some => some.GetHashCode(),
            OptionState.None => Option.None.Value.GetHashCode(),
            _                => 0
        };
    }

    public override string? ToString() =>
        state switch {
            OptionState.Some => some.ToString(),
            OptionState.None => Option.None.Value.ToString(),
            _                => ""
        };

    string DebugPrint() =>
        state switch {
            OptionState.Some => $"Some={some}",
            OptionState.None => "None",
            _                => "Default"
        };

    public Option<TNew> Map<TNew>(Func<T, TNew> map)
        where TNew : notnull =>
        IsSome ? map(some) : default(Option<TNew>);

    public async ValueTask<Option<TNew>> Map<TNew>(Func<T, ValueTask<TNew>> map)
        where TNew : notnull =>
        IsSome ? await map(some).ConfigureAwait(false) : default(Option<TNew>);
}

public static class Option {
    enum ObjectState : byte {
        Default = 0,
        Value = 1
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct Some<T> : IEquatable<Some<T>> {
        public Some() {
            state = ObjectState.Default;
            value = default!;
        }

        public Some(T? value) {
            state = ObjectState.Value;
            this.value = value;
        }

        readonly ObjectState state;
        readonly T? value;

        public T? Value =>
            state switch {
                ObjectState.Value => value,
                _                 => throw OptionHolderExceptions.SomeObjectDefault<T>()
            };

        public static implicit operator Some<T>(T? value) => new(value);
        public static explicit operator T?(Some<T> some) => some.Value;

        public bool Equals(Some<T> other) =>
            (state, other.state) switch {
                (ObjectState.Value, ObjectState.Value) => EqualityComparer<T?>.Default.Equals(value, other.value),
                _                                      => false
            };

        public override bool Equals(object? obj) => obj is Some<T> other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        public static bool operator ==(Some<T> left, Some<T> right) => left.Equals(right);
        public static bool operator !=(Some<T> left, Some<T> right) => left.Equals(right) is false;

        public override string ToString() => Value as string ?? value?.ToString() ?? "";
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct None : IEquatable<None> {
        public static None Value => default;
        public override bool Equals(object? obj) => obj is None;
        public bool Equals(None other) => true;
        public override string ToString() => "()";
        public static implicit operator Unit(None _) => default;
        public static implicit operator None(Unit _) => default;
        public override int GetHashCode() => 0;
        public static bool operator ==(None left, None right) => true;
        public static bool operator !=(None left, None right) => false;
    }
}

public static class GlobalHolderOptionFunctions {
    public static Option.Some<T> Some<T>(T v)
        where T : notnull =>
        new(v);

    public static Option.Some<Unit> Some() => Unit.Value;
    public static Option.None None() => default;
}
