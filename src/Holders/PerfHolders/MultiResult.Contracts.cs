// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders;

public interface IMultiResultHolder {
    // just as base with documentation
}

public enum MultiResultState {
    Default = 0,
    First = 1,
    Second = 2,
    Third = 3
}

public interface IMultiResultHolder<out T1, out T2> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull {
    T1 First { get; }
    T2 Second { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3, out T4> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
    T4 Fourth { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3, out T4, out T5> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
    T4 Fourth { get; }
    T5 Fifth { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3, out T4, out T5, out T6> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
    T4 Fourth { get; }
    T5 Fifth { get; }
    T6 Sixth { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3, out T4, out T5, out T6, out T7> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
    T4 Fourth { get; }
    T5 Fifth { get; }
    T6 Sixth { get; }
    T7 Seventh { get; }
}

public interface IMultiResultHolder<out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8> : IMultiResultHolder
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull
    where T8 : notnull {
    T1 First { get; }
    T2 Second { get; }
    T3 Third { get; }
    T4 Fourth { get; }
    T5 Fifth { get; }
    T6 Sixth { get; }
    T7 Seventh { get; }
    T8 Eighth { get; }
}
