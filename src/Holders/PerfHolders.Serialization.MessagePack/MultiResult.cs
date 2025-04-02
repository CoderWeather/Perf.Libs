// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Serialization.MessagePack;

using System.Linq.Expressions;
using System.Reflection;
using Exceptions;
using global::MessagePack;
using global::MessagePack.Formatters;
using Internal;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class MultiResultHolderFormatterResolver : IFormatterResolver {
    public static readonly MultiResultHolderFormatterResolver Instance = new();
    internal MultiResultHolderFormatterResolver() { }

    public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    static class Cache<T> {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static Cache() {
            var t = typeof(T);
            if (t.IsGenericTypeDefinition
                || t.IsValueType is false
                || t.GetInterfaces().FirstOrDefault(x =>
                    x is {
                        IsConstructedGenericType: true,
                        GenericTypeArguments.Length: >= 2 and <= 8
                    }
                    && x.Name.AsSpan().StartsWith("IMultiResultHolder`".AsSpan())
                ) is not { } marker
            ) {
                Formatter = null;
                return;
            }

            var arguments = marker.GenericTypeArguments;
            var formatterType = arguments.Length switch {
                2 => typeof(MultiResultHolderFormatter<,,>).MakeGenericType([ typeof(T), ..arguments ]),
                3 => typeof(MultiResultHolderFormatter<,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                4 => typeof(MultiResultHolderFormatter<,,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                5 => typeof(MultiResultHolderFormatter<,,,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                6 => typeof(MultiResultHolderFormatter<,,,,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                7 => typeof(MultiResultHolderFormatter<,,,,,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                8 => typeof(MultiResultHolderFormatter<,,,,,,,,>).MakeGenericType([ typeof(T), ..arguments ]),
                _ => throw new InvalidOperationException("Unsupported number of generic arguments"),
            };

            var f = formatterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
            Formatter = (IMessagePackFormatter<T>)f.GetValue(null)!;
        }
    }
}

file static class Ext {
    public static byte GetState<T>(this T mr)
        where T : struct, IMultiResultHolder {
        return MrCache<T>.GetState(mr);
    }

    static class MrCache<T>
        where T : struct, IMultiResultHolder {
        public static readonly Func<T, byte> GetState;

        static MrCache() {
            var p1 = Expression.Parameter(typeof(T), "mr");
            var lambda1 = Expression.Lambda<Func<T, byte>>(
                Expression.Convert(
                    Expression.PropertyOrField(p1, "state"),
                    typeof(byte)
                ),
                p1
            );

            GetState = lambda1.Compile();
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2>
    where T1 : notnull
    where T2 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 2 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 3 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            case 4:
                writer.WriteUInt8(4);
                MessagePackSerializer.Serialize(ref writer, value.Fourth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            case 4: {
                var value = MessagePackSerializer.Deserialize<T4>(ref reader, options);
                var mr = DynamicCast.Cast<T4, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 4 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            case 4:
                writer.WriteUInt8(4);
                MessagePackSerializer.Serialize(ref writer, value.Fourth, options);
                break;
            case 5:
                writer.WriteUInt8(5);
                MessagePackSerializer.Serialize(ref writer, value.Fifth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            case 4: {
                var value = MessagePackSerializer.Deserialize<T4>(ref reader, options);
                var mr = DynamicCast.Cast<T4, TMultiResult>(ref value);
                return mr;
            }
            case 5: {
                var value = MessagePackSerializer.Deserialize<T5>(ref reader, options);
                var mr = DynamicCast.Cast<T5, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 5 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            case 4:
                writer.WriteUInt8(4);
                MessagePackSerializer.Serialize(ref writer, value.Fourth, options);
                break;
            case 5:
                writer.WriteUInt8(5);
                MessagePackSerializer.Serialize(ref writer, value.Fifth, options);
                break;
            case 6:
                writer.WriteUInt8(6);
                MessagePackSerializer.Serialize(ref writer, value.Sixth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            case 4: {
                var value = MessagePackSerializer.Deserialize<T4>(ref reader, options);
                var mr = DynamicCast.Cast<T4, TMultiResult>(ref value);
                return mr;
            }
            case 5: {
                var value = MessagePackSerializer.Deserialize<T5>(ref reader, options);
                var mr = DynamicCast.Cast<T5, TMultiResult>(ref value);
                return mr;
            }
            case 6: {
                var value = MessagePackSerializer.Deserialize<T6>(ref reader, options);
                var mr = DynamicCast.Cast<T6, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 6 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6, T7>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6, T7> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            case 4:
                writer.WriteUInt8(4);
                MessagePackSerializer.Serialize(ref writer, value.Fourth, options);
                break;
            case 5:
                writer.WriteUInt8(5);
                MessagePackSerializer.Serialize(ref writer, value.Fifth, options);
                break;
            case 6:
                writer.WriteUInt8(6);
                MessagePackSerializer.Serialize(ref writer, value.Sixth, options);
                break;
            case 7:
                writer.WriteUInt8(7);
                MessagePackSerializer.Serialize(ref writer, value.Seventh, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            case 4: {
                var value = MessagePackSerializer.Deserialize<T4>(ref reader, options);
                var mr = DynamicCast.Cast<T4, TMultiResult>(ref value);
                return mr;
            }
            case 5: {
                var value = MessagePackSerializer.Deserialize<T5>(ref reader, options);
                var mr = DynamicCast.Cast<T5, TMultiResult>(ref value);
                return mr;
            }
            case 6: {
                var value = MessagePackSerializer.Deserialize<T6>(ref reader, options);
                var mr = DynamicCast.Cast<T6, TMultiResult>(ref value);
                return mr;
            }
            case 7: {
                var value = MessagePackSerializer.Deserialize<T7>(ref reader, options);
                var mr = DynamicCast.Cast<T7, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 7 but got '{key}'");
        }
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6, T7, T8> : IMessagePackFormatter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6, T7, T8>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull
    where T8 : notnull {
    public static readonly MultiResultHolderFormatter<TMultiResult, T1, T2, T3, T4, T5, T6, T7, T8> Instance = new();

    public void Serialize(ref MessagePackWriter writer, TMultiResult value, MessagePackSerializerOptions options) {
        var state = value.GetState();
        writer.WriteMapHeader(1);
        switch (state) {
            case 1:
                writer.WriteUInt8(1);
                MessagePackSerializer.Serialize(ref writer, value.First, options);
                break;
            case 2:
                writer.WriteUInt8(2);
                MessagePackSerializer.Serialize(ref writer, value.Second, options);
                break;
            case 3:
                writer.WriteUInt8(3);
                MessagePackSerializer.Serialize(ref writer, value.Third, options);
                break;
            case 4:
                writer.WriteUInt8(4);
                MessagePackSerializer.Serialize(ref writer, value.Fourth, options);
                break;
            case 5:
                writer.WriteUInt8(5);
                MessagePackSerializer.Serialize(ref writer, value.Fifth, options);
                break;
            case 6:
                writer.WriteUInt8(6);
                MessagePackSerializer.Serialize(ref writer, value.Sixth, options);
                break;
            case 7:
                writer.WriteUInt8(7);
                MessagePackSerializer.Serialize(ref writer, value.Seventh, options);
                break;
            case 8:
                writer.WriteUInt8(8);
                MessagePackSerializer.Serialize(ref writer, value.Eighth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }
    }

    public TMultiResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
            throw new MessagePackSerializationException($"Expected '{MessagePackType.Map}' but got '{reader.NextMessagePackType}'");
        }

        if (mapHeader is not 1) {
            throw new MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
        }

        var key = reader.ReadByte();
        switch (key) {
            case 1: {
                var value = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                var mr = DynamicCast.Cast<T1, TMultiResult>(ref value);
                return mr;
            }
            case 2: {
                var value = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                var mr = DynamicCast.Cast<T2, TMultiResult>(ref value);
                return mr;
            }
            case 3: {
                var value = MessagePackSerializer.Deserialize<T3>(ref reader, options);
                var mr = DynamicCast.Cast<T3, TMultiResult>(ref value);
                return mr;
            }
            case 4: {
                var value = MessagePackSerializer.Deserialize<T4>(ref reader, options);
                var mr = DynamicCast.Cast<T4, TMultiResult>(ref value);
                return mr;
            }
            case 5: {
                var value = MessagePackSerializer.Deserialize<T5>(ref reader, options);
                var mr = DynamicCast.Cast<T5, TMultiResult>(ref value);
                return mr;
            }
            case 6: {
                var value = MessagePackSerializer.Deserialize<T6>(ref reader, options);
                var mr = DynamicCast.Cast<T6, TMultiResult>(ref value);
                return mr;
            }
            case 7: {
                var value = MessagePackSerializer.Deserialize<T7>(ref reader, options);
                var mr = DynamicCast.Cast<T7, TMultiResult>(ref value);
                return mr;
            }
            case 8: {
                var value = MessagePackSerializer.Deserialize<T8>(ref reader, options);
                var mr = DynamicCast.Cast<T8, TMultiResult>(ref value);
                return mr;
            }
            default: throw new MessagePackSerializationException($"Expected key 1 to 3 but got '{key}'");
        }
    }
}
