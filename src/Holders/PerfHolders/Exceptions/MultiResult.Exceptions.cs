namespace Perf.Holders.Exceptions;

public abstract class MultiResultException(string message) : Exception(message);
public sealed class MultiResultDefaultException(string message) : MultiResultException(message);
public sealed class MultiResultWrongAccessException(string message) : MultiResultException(message);

public static class MultiResultHolderExceptions {
    public static MultiResultDefaultException Default<T>()
        where T : struct, IMultiResultHolder =>
        new($"{typeof(T)} Cannot access values while state is Default");

    public static MultiResultWrongAccessException WrongAccess<T>(string accessedState, string expectedState)
        where T : struct, IMultiResultHolder =>
        new($"{typeof(T)} Cannot access {accessedState} while state is {expectedState}");
}
