namespace Perf.Holders;

public abstract class OptionHolderException(string message) : Exception(message);
public sealed class OptionStateOutOfValidValues(string message) : OptionHolderException(message);
public sealed class OptionSomeAccessWhenNoneException(string message) : OptionHolderException(message);

public static class OptionHolderExceptions {
    public static OptionStateOutOfValidValues StateOutOfValidValues<TOption, TSome>(OptionState state)
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new(
            $"{typeof(TOption)} OptionState '{(byte)state}' is out of valid values"
        );

    public static OptionSomeAccessWhenNoneException SomeAccessWhenNone<TOption, TSome>()
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new(
            $"{typeof(TOption)} Cannot access Some while state is None"
        );
}
