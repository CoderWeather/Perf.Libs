namespace Perf.Holders.Serialization.MessagePack;

using System.Collections.Concurrent;
using System.Reflection;
using global::MessagePack;
using global::MessagePack.Formatters;
using Option;

public sealed class OptionHolderFormatterResolver : IFormatterResolver {
    public static readonly OptionHolderFormatterResolver Instance = new();

    static readonly ConcurrentDictionary<Type, IMessagePackFormatter> Converters = new();

    public IMessagePackFormatter<T>? GetFormatter<T>() {
        var t = typeof(T);
        if (t.IsGenericTypeDefinition
         || t.IsValueType is false
         || t.GetInterface("IOptionHolder`1") is null) {
            return null;
        }

        if (Converters.TryGetValue(t, out var formatter)) {
            return (IMessagePackFormatter<T>)formatter;
        }

        var i = t.GetInterface("IOptionHolder`1")!;
        var arg1 = i.GenericTypeArguments[0];

        var t2 = typeof(OptionHolderFormatter<,>).MakeGenericType(t, arg1);
        var f = t2.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[t] = formatter = (IMessagePackFormatter)f.GetValue(null)!;
        return (IMessagePackFormatter<T>)formatter;
    }
}

sealed class OptionHolderFormatter<TOption, TValue> : IMessagePackFormatter<TOption>
    where TOption : struct, IOptionHolder<TValue> {
    private OptionHolderFormatter() { }
    public static readonly OptionHolderFormatter<TOption, TValue> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TOption value, MessagePackSerializerOptions options) {
        if (value.IsSome) {
            MessagePackSerializer.Serialize(ref writer, value.Some, options);
        } else {
            writer.WriteNil();
        }
    }

    public TOption Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil) {
            reader.ReadNil();
            return default;
        }

        var value = MessagePackSerializer.Deserialize<TValue>(ref reader, options);
        return DynamicCast.Cast<TValue, TOption>(ref value);
    }
}
