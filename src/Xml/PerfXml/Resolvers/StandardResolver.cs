namespace PerfXml.Resolvers;

public sealed class StandardResolver : IXmlFormatterResolver {
    public static readonly StandardResolver Instance = new();
    StandardResolver() { }

#region IXmlFormatterResolver Members

    public IXmlFormatter<T>? GetFormatter<T>() {
        return Cache<T>.Formatter;
    }

#endregion

#region Nested type: Cache

    static class Cache<T> {
        public static readonly IXmlFormatter<T>? Formatter;

        static Cache() {
            Formatter ??= SystemResolver.Instance.GetFormatter<T>();
            Formatter ??= EnumResolver.Instance.GetFormatter<T>();
            Formatter ??= NullableStructFormatterResolver.Instance.GetFormatter<T>();
        }
    }

#endregion
}
