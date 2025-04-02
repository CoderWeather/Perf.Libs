// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Serialization.MessagePack;

using System.Reflection;
using global::MessagePack;
using global::MessagePack.Formatters;
using Internal;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class OptionHolderFormatterResolver : IFormatterResolver {
    public static readonly OptionHolderFormatterResolver Instance = new();

    public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    static class Cache<T> {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static Cache() {
            var t = typeof(T);
            if (t.IsGenericTypeDefinition || t.IsValueType is false || t.GetInterface("IOptionHolder`1") is not { } i) {
                Formatter = null;
                return;
            }

            var arg1 = i.GenericTypeArguments[0];

            var t2 = typeof(OptionHolderFormatter<,>).MakeGenericType(t, arg1);
            var f = t2.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
            Formatter = (IMessagePackFormatter<T>)f.GetValue(null)!;
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class OptionHolderFormatter<TOption, TValue> : IMessagePackFormatter<TOption>
    where TOption : struct, IOptionHolder<TValue>
    where TValue : notnull {
    public static readonly OptionHolderFormatter<TOption, TValue> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TOption value, MessagePackSerializerOptions options) {
        if (value.IsSome) {
            MessagePackSerializer.Serialize(ref writer, value.Some, options);
        } else {
            writer.WriteNil();
        }
    }

    public TOption Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.TryReadNil()) {
            return default;
        }

        var value = MessagePackSerializer.Deserialize<TValue>(ref reader, options);
        return DynamicCast.Cast<TValue, TOption>(ref value);
    }
}
