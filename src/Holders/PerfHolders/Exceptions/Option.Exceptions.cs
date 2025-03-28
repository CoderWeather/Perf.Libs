namespace Perf.Holders.Exceptions;

public abstract class OptionSomeException(string message) : Exception(message);
public sealed class OptionSomeUninitializedException(string message) : OptionSomeException(message);
public sealed class OptionSomeStateOutOfValidValuesException(string message) : OptionSomeException(message);
public abstract class OptionHolderException(string message) : Exception(message);
public sealed class OptionStateOutOfValidValues(string message) : OptionHolderException(message);
public sealed class OptionSomeAccessWhenNoneException(string message) : OptionHolderException(message);

public static class OptionHolderExceptions {
    public static OptionSomeUninitializedException SomeUnitialized<TSome>() =>
        new($"{typeof(Option.Some<TSome>)} Cannot access state while state is Unitilized");

    public static OptionSomeStateOutOfValidValuesException SomeStateOutOfValidValues<TSome>(byte state) =>
        new($"{typeof(Option.Some<TSome>)} SomeState {state} is out of valid values");

    public static OptionStateOutOfValidValues StateOutOfValidValues<TOption, TSome>(OptionState state)
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new($"{typeof(TOption)} OptionState {(byte)state} is out of valid values");

    public static OptionSomeAccessWhenNoneException SomeAccessWhenNone<TOption, TSome>()
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new($"{typeof(TOption)} Cannot access Some while state is None");
}
