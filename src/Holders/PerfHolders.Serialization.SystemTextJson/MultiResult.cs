// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Serialization.SystemTextJson;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptions;
using Internal;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MultiResultHolderJsonConverterFactory : JsonConverterFactory {
    public static readonly MultiResultHolderJsonConverterFactory Instance = new();
    internal MultiResultHolderJsonConverterFactory() { }

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericTypeDefinition is false
        && typeToConvert.IsValueType
        && typeToConvert.GetInterfaces().Any(x =>
            x.IsConstructedGenericType
            && x.GenericTypeArguments.Length is >= 2 and <= 8
            && x.Name.AsSpan().StartsWith("IMultiResultHolder`".AsSpan())
        );

    static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (Converters.TryGetValue(typeToConvert, out var converter)) {
            return converter;
        }

        var marker = typeToConvert.GetInterfaces().First(x =>
            x.IsConstructedGenericType
            && x.GenericTypeArguments.Length is >= 2 and <= 8
            && x.Name.AsSpan().StartsWith("IMultiResultHolder`".AsSpan())
        );

        var arguments = marker.GenericTypeArguments;

        var converterType = arguments.Length switch {
            2 => typeof(MultiResultHolderJsonConverter<,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            3 => typeof(MultiResultHolderJsonConverter<,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            4 => typeof(MultiResultHolderJsonConverter<,,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            5 => typeof(MultiResultHolderJsonConverter<,,,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            6 => typeof(MultiResultHolderJsonConverter<,,,,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            7 => typeof(MultiResultHolderJsonConverter<,,,,,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            8 => typeof(MultiResultHolderJsonConverter<,,,,,,,,>).MakeGenericType([ typeToConvert, ..arguments ]),
            _ => throw new InvalidOperationException("Unsupported number of generic arguments"),
        };

        var f = converterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
        return converter;
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
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2>
    where T1 : notnull
    where T2 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            case 4:
                writer.WritePropertyName("fourth"u8);
                JsonSerializer.Serialize(writer, value.Fourth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fourth"u8)) {
            var value = JsonSerializer.Deserialize<T4>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T4, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            case 4:
                writer.WritePropertyName("fourth"u8);
                JsonSerializer.Serialize(writer, value.Fourth, options);
                break;
            case 5:
                writer.WritePropertyName("fifth"u8);
                JsonSerializer.Serialize(writer, value.Fifth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fourth"u8)) {
            var value = JsonSerializer.Deserialize<T4>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T4, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fifth"u8)) {
            var value = JsonSerializer.Deserialize<T5>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T5, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            case 4:
                writer.WritePropertyName("fourth"u8);
                JsonSerializer.Serialize(writer, value.Fourth, options);
                break;
            case 5:
                writer.WritePropertyName("fifth"u8);
                JsonSerializer.Serialize(writer, value.Fifth, options);
                break;
            case 6:
                writer.WritePropertyName("sixth"u8);
                JsonSerializer.Serialize(writer, value.Sixth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fourth"u8)) {
            var value = JsonSerializer.Deserialize<T4>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T4, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fifth"u8)) {
            var value = JsonSerializer.Deserialize<T5>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T5, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("sixth"u8)) {
            var value = JsonSerializer.Deserialize<T6>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T6, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6, T7> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6, T7>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6, T7> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            case 4:
                writer.WritePropertyName("fourth"u8);
                JsonSerializer.Serialize(writer, value.Fourth, options);
                break;
            case 5:
                writer.WritePropertyName("fifth"u8);
                JsonSerializer.Serialize(writer, value.Fifth, options);
                break;
            case 6:
                writer.WritePropertyName("sixth"u8);
                JsonSerializer.Serialize(writer, value.Sixth, options);
                break;
            case 7:
                writer.WritePropertyName("seventh"u8);
                JsonSerializer.Serialize(writer, value.Seventh, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fourth"u8)) {
            var value = JsonSerializer.Deserialize<T4>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T4, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fifth"u8)) {
            var value = JsonSerializer.Deserialize<T5>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T5, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("sixth"u8)) {
            var value = JsonSerializer.Deserialize<T6>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T6, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("seventh"u8)) {
            var value = JsonSerializer.Deserialize<T7>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T7, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6, T7, T8> : JsonConverter<TMultiResult>
    where TMultiResult : struct, IMultiResultHolder<T1, T2, T3, T4, T5, T6, T7, T8>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
    where T6 : notnull
    where T7 : notnull
    where T8 : notnull {
    public static readonly MultiResultHolderJsonConverter<TMultiResult, T1, T2, T3, T4, T5, T6, T7, T8> Instance = new();

    public override void Write(Utf8JsonWriter writer, TMultiResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        var state = value.GetState();
        switch (state) {
            case 1:
                writer.WritePropertyName("first"u8);
                JsonSerializer.Serialize(writer, value.First, options);
                break;
            case 2:
                writer.WritePropertyName("second"u8);
                JsonSerializer.Serialize(writer, value.Second, options);
                break;
            case 3:
                writer.WritePropertyName("third"u8);
                JsonSerializer.Serialize(writer, value.Third, options);
                break;
            case 4:
                writer.WritePropertyName("fourth"u8);
                JsonSerializer.Serialize(writer, value.Fourth, options);
                break;
            case 5:
                writer.WritePropertyName("fifth"u8);
                JsonSerializer.Serialize(writer, value.Fifth, options);
                break;
            case 6:
                writer.WritePropertyName("sixth"u8);
                JsonSerializer.Serialize(writer, value.Sixth, options);
                break;
            case 7:
                writer.WritePropertyName("seventh"u8);
                JsonSerializer.Serialize(writer, value.Seventh, options);
                break;
            case 8:
                writer.WritePropertyName("eighth"u8);
                JsonSerializer.Serialize(writer, value.Eighth, options);
                break;
            default: throw MultiResultHolderExceptions.Default<TMultiResult>();
        }

        writer.WriteEndObject();
    }

    public override TMultiResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }

        reader.Read();

        var span = reader.ValueSpan;
        if (span.SequenceEqual("first"u8)) {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T1, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("second"u8)) {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T2, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("third"u8)) {
            var value = JsonSerializer.Deserialize<T3>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T3, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fourth"u8)) {
            var value = JsonSerializer.Deserialize<T4>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T4, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("fifth"u8)) {
            var value = JsonSerializer.Deserialize<T5>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T5, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("sixth"u8)) {
            var value = JsonSerializer.Deserialize<T6>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T6, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("seventh"u8)) {
            var value = JsonSerializer.Deserialize<T7>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T7, TMultiResult>(ref value);
            return result;
        }

        if (span.SequenceEqual("eighth"u8)) {
            var value = JsonSerializer.Deserialize<T8>(ref reader, options)!;
            reader.Read();
            var result = DynamicCast.Cast<T8, TMultiResult>(ref value);
            return result;
        }

        throw new JsonException("Expected MultiResult ordinal field name property");
    }
}
