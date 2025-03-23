// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders;

public static class OptionExtensions {
    public static Option<T> AsOption<T>(this T? value)
        where T : struct =>
        value.HasValue ? new(value.Value) : default;

    public static Option<T> AsOption<T>(this T? value) => new(value);
}
