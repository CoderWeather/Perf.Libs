namespace GeneratorTester.Proposals;

public interface IMultiResultHolder<out T1, out T2, out T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    T1 First { get; }
    bool IsFirst { get; }
    T2 Second { get; }
    bool IsSecond { get; }
    T3 Third { get; }
    bool IsThird { get; }
    MultiResultState State { get; }
}

public enum MultiResultState {
    Uninitialized = 0,
    First = 1,
    Second = 2,
    Third = 3
}
