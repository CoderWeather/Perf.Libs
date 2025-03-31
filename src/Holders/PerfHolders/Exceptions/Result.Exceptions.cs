// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Exceptions;

public abstract class ResultObjectException(string message) : ResultHolderException(message);
public sealed class ResultObjectDefaultException(string message) : ResultObjectException(message);
public abstract class ResultHolderException(string message) : Exception(message);
public sealed class ResultDefaultException(string message) : ResultHolderException(message);
public sealed class ResultWrongAccessException(string message) : ResultHolderException(message);

public static class ResultHolderExceptions {
    public static ResultObjectDefaultException OkObjectDefault<TOk>()
        where TOk : notnull =>
        new($"{typeof(Result.Ok<TOk>)} Cannot access values while state is Default");

    public static ResultObjectDefaultException ErrorObjectDefault<TError>()
        where TError : notnull =>
        new($"{typeof(Result.Error<TError>)} Cannot access values while state is Default");

    public static ResultDefaultException Default<TResult, TOk, TError>()
        where TResult : struct, IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new($"{typeof(TResult)} Cannot access values while state is Default");

    public static ResultWrongAccessException WrongAccess<TResult, TOk, TError>(string accessedState, string expectedState)
        where TResult : struct, IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new($"{typeof(TResult)} Cannot access {accessedState} while state is {expectedState}");
}
