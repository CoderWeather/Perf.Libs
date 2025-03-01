using MessagePack;
using MessagePack.Formatters;
using Utilities.Generic;

namespace Utilities.MsgPack;

public sealed class BasicFilterFormatterResolver : IFormatterResolver {
    private BasicFilterFormatterResolver() { }
    public static readonly BasicFilterFormatterResolver Instance = new();

    public IMessagePackFormatter<T>? GetFormatter<T>() {
        var t = typeof(T);

        if (t.Name is "BasicFilter`1" && t.IsConstructedGenericType) {
            var argType = t.GetGenericArguments()[0];
            if (argType.IsGenericTypeDefinition is false) {
                return Cache<T>.Formatter;
            }
        }

        return null;
    }

    private static class Cache<T> {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static Cache() {
            var filterType = typeof(T);
            var arg = filterType.GetGenericArguments()[0];

            var formatter = typeof(BasicFilterFormatter<>).MakeGenericType(arg);
            var field = formatter.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
            Formatter = field.GetValue(null) as IMessagePackFormatter<T>;
        }
    }
}

internal sealed class BasicFilterFormatter<T> : IMessagePackFormatter<BasicFilter<T>?> where T : notnull {
    private BasicFilterFormatter() { }
    public static readonly BasicFilterFormatter<T> Instance = new();

    public void Serialize(ref MessagePackWriter writer, BasicFilter<T>? value, MessagePackSerializerOptions options) {
        if (value is null) {
            writer.WriteNil();
            return;
        }

        MessagePackSerializer.Serialize(ref writer, value.Entries, options);
    }

    public BasicFilter<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        var entries = MessagePackSerializer.Deserialize<FilterEntry[]>(ref reader, options);
        if (entries is not null) {
            return new(entries);
        }

        return null;
    }
}
