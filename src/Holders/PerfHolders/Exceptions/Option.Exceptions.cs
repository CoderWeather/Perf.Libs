// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Exceptions;

public abstract class OptionSomeException(string message) : OptionHolderException(message);
public sealed class OptionObjectDefaultException(string message) : OptionSomeException(message);
public abstract class OptionHolderException(string message) : Exception(message);
public sealed class OptionDefaultException(string message) : OptionHolderException(message);
public sealed class OptionSomeAccessWhenNoneException(string message) : OptionHolderException(message);

public static class OptionHolderExceptions {
    public static OptionObjectDefaultException SomeObjectDefault<TSome>() => new($"{typeof(Option.Some<TSome>)} Cannot access values while state is Default");

    public static OptionDefaultException Default<TOption, TSome>()
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new($"{typeof(TOption)} Cannot access values while state is Default");

    public static OptionSomeAccessWhenNoneException SomeAccessWhenNone<TOption, TSome>(string someString)
        where TOption : struct, IOptionHolder<TSome>
        where TSome : notnull =>
        new($"{typeof(TOption)} Cannot access {someString} while state is None");
}
