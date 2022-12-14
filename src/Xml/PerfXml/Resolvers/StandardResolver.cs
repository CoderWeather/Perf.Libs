namespace PerfXml.Resolvers;

public sealed class StandardResolver : IXmlFormatterResolver {
    public static readonly StandardResolver Instance = new();
    StandardResolver() { }

    public IXmlFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    static class Cache<T> {
        public static readonly IXmlFormatter<T>? Formatter;

        static Cache() {
            Formatter ??= SystemResolver.Instance.GetFormatter<T>();
            Formatter ??= EnumResolver.Instance.GetFormatter<T>();
            Formatter ??= NullableStructFormatterResolver.Instance.GetFormatter<T>();
        }
    }
}
