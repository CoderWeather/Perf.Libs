// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Serialization.MessagePack;

using System.Collections.Concurrent;
using System.Reflection;
using global::MessagePack;
using global::MessagePack.Formatters;
using Internal;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class ResultHolderFormatterResolver : IFormatterResolver {
    public static readonly ResultHolderFormatterResolver Instance = new();

    static readonly ConcurrentDictionary<Type, IMessagePackFormatter> Converters = new();

    public IMessagePackFormatter<T>? GetFormatter<T>() {
        var t = typeof(T);
        if (t.IsGenericTypeDefinition || t.IsValueType is false || t.GetInterface("IResultHolder`2") is not { } i) {
            return null;
        }

        if (Converters.TryGetValue(t, out var formatter)) {
            return (IMessagePackFormatter<T>)formatter;
        }

        var arg1 = i.GenericTypeArguments[0];
        var arg2 = i.GenericTypeArguments[1];

        var t2 = typeof(HolderResultFormatter<,,>).MakeGenericType(t, arg1, arg2);
        var f = t2.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[t] = formatter = (IMessagePackFormatter)f.GetValue(null)!;
        return (IMessagePackFormatter<T>)formatter;
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class HolderResultFormatter<TResult, TOk, TError> : IMessagePackFormatter<TResult>
    where TResult : struct, IResultHolder<TOk, TError>
    where TOk : notnull
    where TError : notnull {
    public static readonly HolderResultFormatter<TResult, TOk, TError> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TResult value, MessagePackSerializerOptions options) {
        // catch to throwing exceptions any states other than ok and error
        var isOk = value.IsOk;
        writer.WriteMapHeader(1);
        if (isOk) {
            writer.WriteUInt8(1);
            MessagePackSerializer.Serialize(ref writer, value.Ok, options);
        } else {
            writer.WriteUInt8(2);
            MessagePackSerializer.Serialize(ref writer, value.Error, options);
        }
    }

    public TResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader)) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<TOk>(ref reader, options);
                return DynamicCast.Cast<TOk, TResult>(ref value);
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<TError>(ref reader, options);
                return DynamicCast.Cast<TError, TResult>(ref value);
            }
            default: throw new MessagePackSerializationException($"Expected key 1 or 2 but got '{key}'");
        }
    }
}
