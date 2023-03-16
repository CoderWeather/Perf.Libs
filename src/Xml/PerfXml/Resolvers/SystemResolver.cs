using PerfXml.Formatters;

namespace PerfXml.Resolvers;

public sealed class SystemResolver : IXmlFormatterResolver {
    public static readonly SystemResolver Instance = new();

    static readonly Dictionary<Type, IXmlFormatter> Formatters = new() {
        { typeof(byte), ByteFormatter.Instance },
        { typeof(short), Int16Formatter.Instance },
        { typeof(int), Int32Formatter.Instance },
        { typeof(uint), UInt32Formatter.Instance },
        { typeof(long), Int64Formatter.Instance },
        { typeof(double), DoubleFormatter.Instance },
        { typeof(decimal), DecimalFormatter.Instance },
        { typeof(char), CharFormatter.Instance },
        { typeof(bool), BooleanFormatter.Instance },
        { typeof(string), StringFormatter.Instance },
        { typeof(Guid), GuidFormatter.Instance },
        { typeof(DateTime), DateTimeFormatter.Instance },
#if NET6_0_OR_GREATER
        { typeof(DateOnly), DateOnlyFormatter.Instance },
        { typeof(TimeOnly), TimeOnlyFormatter.Instance },
#endif
    };

    SystemResolver() { }

#region IXmlFormatterResolver Members

    public IXmlFormatter<T>? GetFormatter<T>() {
        return Cache<T>.Formatter;
    }

#endregion

#region Nested type: Cache

    static class Cache<T> {
        public static readonly IXmlFormatter<T>? Formatter;

        static Cache() {
            if (Formatters.TryGetValue(typeof(T), out var formatter)) {
                Formatter = (IXmlFormatter<T>)formatter;
            }
        }
    }

#endregion
}
