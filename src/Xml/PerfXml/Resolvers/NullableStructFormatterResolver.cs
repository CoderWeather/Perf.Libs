﻿using PerfXml.Formatters;

namespace PerfXml.Resolvers;

public sealed class NullableStructFormatterResolver : IXmlFormatterResolver {
    public static readonly NullableStructFormatterResolver Instance = new();
    static readonly Type NullableType = typeof(Nullable<>);
    static readonly Type NullableStructFormatterType = typeof(NullableStructFormatter<>);
    NullableStructFormatterResolver() { }

#region IXmlFormatterResolver Members

    public IXmlFormatter<T>? GetFormatter<T>() {
        return Cache<T>.Formatter;
    }

#endregion

#region Nested type: Cache

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

#endregion
}
