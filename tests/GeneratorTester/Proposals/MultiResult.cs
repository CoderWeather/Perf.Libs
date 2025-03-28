// ReSharper disable MemberCanBePrivate.Local

namespace GeneratorTester.Proposals;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
            _                       => "!!! Incorrect State !!!"
        };
    }

    public MultiResultState State { get; }
    public object? Value { get; }
}

[DebuggerTypeProxy(typeof(MultiResultHolder_DebugView<,,>))]
[SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
readonly struct MultiResult<T1, T2, T3> : IMultiResultHolder<T1, T2, T3>,
    IEquatable<MultiResult<T1, T2, T3>>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    public MultiResult() {
        t1 = default!;
        t2 = default!;
        t3 = default!;
        state = MultiResultState.Uninitialized;
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

    readonly T1 t1;
    readonly T2 t2;
    readonly T3 t3;
    readonly MultiResultState state;

    public T1 First =>
        state switch {
            MultiResultState.First => t1,
            _                      => throw new()
        };

    public bool IsFirst => state == MultiResultState.First;

    public T2 Second =>
        state switch {
            MultiResultState.Second => t2,
            _                       => throw new()
        };

    public bool IsSecond => state == MultiResultState.Second;

    public T3 Third =>
        state switch {
            MultiResultState.Third => t3,
            _                      => throw new()
        };

    public bool IsThird => state == MultiResultState.Third;
    public MultiResultState State => state;

    public override bool Equals(object? obj) => obj is MultiResult<T1, T2, T3> other && Equals(other);

    public bool Equals(MultiResult<T1, T2, T3> other) =>
        (state, other.state) switch {
            (MultiResultState.First, MultiResultState.First)   => EqualityComparer<T1?>.Default.Equals(t1, other.t1),
            (MultiResultState.Second, MultiResultState.Second) => EqualityComparer<T2?>.Default.Equals(t2, other.t2),
            (MultiResultState.Third, MultiResultState.Third)   => EqualityComparer<T3?>.Default.Equals(t3, other.t3),
            _                                                  => false
        };

    public override int GetHashCode() =>
        state switch {
            MultiResultState.First  => t1.GetHashCode(),
            MultiResultState.Second => t2.GetHashCode(),
            MultiResultState.Third  => t3.GetHashCode(),
            _                       => throw new()
        };
}
