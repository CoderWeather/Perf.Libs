// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders;

using System.Diagnostics;
using System.Runtime.InteropServices;

file sealed class MultiResultHolder_DebugView<T1, T2, T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    public MultiResultHolder_DebugView(MultiResult<T1, T2, T3> multiResult) {
        State = multiResult.State;
        Value = State switch {
            MultiResultState.First  => multiResult.First,
            MultiResultState.Second => multiResult.Second,
            MultiResultState.Third  => multiResult.Third,
            _                       => "Default"
        };
    }

    public MultiResultState State { get; }
    public object Value { get; }
}

[DebuggerTypeProxy(typeof(MultiResultHolder_DebugView<,,>))]
[DebuggerDisplay("{DebugPrint()}")]
[StructLayout(LayoutKind.Auto)]
readonly struct MultiResult<T1, T2, T3> : IMultiResultHolder<T1, T2, T3>, IEquatable<MultiResult<T1, T2, T3>>
// в типах с generic аргументами нельзя выставлять IEquatable под generic аргументы чтобы имплементации не пересекались в рантайме
// возможно добавить дополнительные IEquatable в генерированных классах
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    public MultiResult() {
        t1 = default!;
        t2 = default!;
        t3 = default!;
        state = MultiResultState.Default;
    }

    public MultiResult(T1 first) {
        t1 = first;
        t2 = default!;
        t3 = default!;
        state = MultiResultState.First;
    }

    public MultiResult(T2 second) {
        t1 = default!;
        t2 = second;
        t3 = default!;
        state = MultiResultState.Second;
    }

    public MultiResult(T3 third) {
        t1 = default!;
        t2 = default!;
        t3 = third;
        state = MultiResultState.Third;
    }

    readonly MultiResultState state;
    readonly T1 t1;
    readonly T2 t2;
    readonly T3 t3;

    public T1 First =>
        state switch {
            MultiResultState.First  => t1,
            MultiResultState.Second => throw new("2"),
            MultiResultState.Third  => throw new("3"),
            _                       => throw new("Default")
        };

    public bool IsFirst => state is MultiResultState.First;

    public T2 Second =>
        state switch {
            MultiResultState.First  => throw new("1"),
            MultiResultState.Second => t2,
            MultiResultState.Third  => throw new("3"),
            _                       => throw new("Default")
        };

    public bool IsSecond => state is MultiResultState.Second;

    public T3 Third =>
        state switch {
            MultiResultState.First  => throw new("1"),
            MultiResultState.Second => throw new("2"),
            MultiResultState.Third  => t3,
            _                       => throw new("Default")
        };

    public bool IsThird => state is MultiResultState.Third;

    public MultiResultState State => state;

    public static implicit operator MultiResult<T1, T2, T3>(T1 t1) => new(t1);
    public static implicit operator MultiResult<T1, T2, T3>(T2 t2) => new(t2);
    public static implicit operator MultiResult<T1, T2, T3>(T3 t3) => new(t3);
    public static bool operator ==(MultiResult<T1, T2, T3> left, MultiResult<T1, T2, T3> right) => left.Equals(right);
    public static bool operator !=(MultiResult<T1, T2, T3> left, MultiResult<T1, T2, T3> right) => left.Equals(right) is false;
    public static bool operator ==(MultiResult<T1, T2, T3> left, T1 right) => left.Equals(right);
    public static bool operator !=(MultiResult<T1, T2, T3> left, T1 right) => left.Equals(right) is false;
    public static bool operator ==(MultiResult<T1, T2, T3> left, T2 right) => left.Equals(right);
    public static bool operator !=(MultiResult<T1, T2, T3> left, T2 right) => left.Equals(right) is false;
    public static bool operator ==(MultiResult<T1, T2, T3> left, T3 right) => left.Equals(right);
    public static bool operator !=(MultiResult<T1, T2, T3> left, T3 right) => left.Equals(right) is false;

    // public ref TOther CastByRef<TOther>()
    //     where TOther : IMultiResultHolder<T1, T2, T3> {
    //     return ref Unsafe.As<MultiResult<T1, T2, T3>, TOther>(ref Unsafe.AsRef(in this));
    // }

    public override bool Equals(object? obj) {
        return obj switch {
            MultiResult<T1, T2, T3> mr => Equals(mr),
            T1 t                       => Equals(t),
            T2 t                       => Equals(t),
            T3 t                       => Equals(t),
            _                          => false
        };
    }

    public bool Equals(MultiResult<T1, T2, T3> other) =>
        (state, other.state) switch {
            (MultiResultState.First, MultiResultState.First)   => EqualityComparer<T1?>.Default.Equals(t1, other.t1),
            (MultiResultState.Second, MultiResultState.Second) => EqualityComparer<T2?>.Default.Equals(t2, other.t2),
            (MultiResultState.Third, MultiResultState.Third)   => EqualityComparer<T3?>.Default.Equals(t3, other.t3),
            _                                                  => false
        };

    public bool Equals(T1? other) => IsFirst && EqualityComparer<T1?>.Default.Equals(t1, other);
    public bool Equals(T2? other) => IsSecond && EqualityComparer<T2?>.Default.Equals(t2, other);
    public bool Equals(T3? other) => IsThird && EqualityComparer<T3?>.Default.Equals(t3, other);

    public override int GetHashCode() =>
        state switch {
            MultiResultState.First  => t1.GetHashCode(),
            MultiResultState.Second => t2.GetHashCode(),
            MultiResultState.Third  => t3.GetHashCode(),
            _                       => 0
        };

    public override string? ToString() =>
        state switch {
            MultiResultState.First  => t1.ToString(),
            MultiResultState.Second => t2.ToString(),
            MultiResultState.Third  => t3.ToString(),
            _                       => ""
        };

    string DebugPrint() =>
        state switch {
            MultiResultState.First  => $"First={t1}",
            MultiResultState.Second => $"Second={t2}",
            MultiResultState.Third  => $"Third={t3}",
            _                       => "Default"
        };
}
