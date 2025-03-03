namespace Perf.Holders;

public abstract class ResultHolderException(string message) : Exception(message);
public sealed class ResultStateOutOfValidValuesException(string message) : ResultHolderException(message);
public sealed class ResultUninitializedException(string message) : ResultHolderException(message);
public sealed class ResultOkAccessWhenErrorException(string message) : ResultHolderException(message);
public sealed class ResultErrorAccessWhenOkException(string message) : ResultHolderException(message);

public static class ResultHolderExceptions {
    public static ResultStateOutOfValidValuesException StateOutOfValidValues<TResult, TOk, TError>(ResultState state)
        where TResult : IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new(
            $"{typeof(TResult)} ResultState '{(byte)state}' is out of valid values"
        );

    public static ResultUninitializedException Uninitialized<TResult, TOk, TError>()
        where TResult : IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new(
            $"{typeof(TResult)} Cannot access state while state is {ResultState.Uninitialized}"
        );

    public static ResultOkAccessWhenErrorException OkAccessWhenError<TResult, TOk, TError>(string okState, string errorState)
        where TResult : IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new(
            $"{typeof(TResult)} Cannot access {okState} while state is {errorState}"
        );

    public static ResultErrorAccessWhenOkException ErrorAccessWhenOk<TResult, TOk, TError>(string okState, string errorState)
        where TResult : IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull =>
        new(
            $"{typeof(TResult)} Cannot access {errorState} while state is {okState}"
        );
}
