using PerfXml.Formatters;

namespace PerfXml.Resolvers;

public sealed class NullableStructFormatterResolver : IXmlFormatterResolver {
    public static readonly NullableStructFormatterResolver Instance = new();
    static readonly Type NullableType = typeof(Nullable<>);
    static readonly Type NullableStructFormatterType = typeof(NullableStructFormatter<>);
    NullableStructFormatterResolver() { }

    public IXmlFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    static class Cache<T> {
        public static readonly IXmlFormatter<T>? Formatter;

        static Cache() {
            var type = typeof(T);
            if (type.IsValueType is false
             || type.IsConstructedGenericType is false
             || type.GetGenericTypeDefinition() != NullableType) {
                return;
            }

            var resultType = type.GenericTypeArguments.First();
            var instance = Activator.CreateInstance(NullableStructFormatterType.MakeGenericType(resultType));
            Formatter = instance as IXmlFormatter<T>;
        }
    }
}
