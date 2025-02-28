namespace Perf.Holders.Serialization.SystemTextJson;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
public sealed class ResultHolderJsonConverterFactory : JsonConverterFactory {
    public static readonly ResultHolderJsonConverterFactory Instance = new();

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsGenericTypeDefinition is false
         && typeToConvert.IsValueType
         && typeToConvert.GetInterface("IResultHolder`2") is not null;
    }

    private static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new();

#if NET7_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (Converters.TryGetValue(typeToConvert, out var converter)) {
            return converter;
        }

        var i = typeToConvert.GetInterface("IResultHolder`2")!;
        var arg1 = i.GenericTypeArguments[0];
        var arg2 = i.GenericTypeArguments[1];

        var t = typeof(HolderResultJsonConverter<,,>).MakeGenericType(typeToConvert, arg1, arg2);
        var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
        return converter;
    }
}

sealed class HolderResultJsonConverter<TResult, TOk, TError> : JsonConverter<TResult>
    where TResult : struct, IResultHolder<TOk, TError>
    where TOk : notnull
    where TError : notnull {
    private HolderResultJsonConverter() { }
    public static readonly HolderResultJsonConverter<TResult, TOk, TError> Instance = new();

    public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        if (value.IsOk) {
            writer.WritePropertyName("ok");
            JsonSerializer.Serialize(writer, value.Ok, options);
        } else {
            writer.WritePropertyName("error");
            JsonSerializer.Serialize(writer, value.Error, options);
        }
        writer.WriteEndObject();
    }

    public override TResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected '{JsonTokenType.StartObject}' but got '{reader.TokenType}'");
        }
        reader.Read();

        if (reader.ValueSpan.SequenceEqual("ok"u8)) {
            var value = JsonSerializer.Deserialize<TOk>(ref reader, options)!;
            reader.Read();
            return DynamicCast.Cast<TOk, TResult>(ref value);
        }
        if (reader.ValueSpan.SequenceEqual("error"u8)) {
            var value = JsonSerializer.Deserialize<TError>(ref reader, options)!;
            reader.Read();
            return DynamicCast.Cast<TError, TResult>(ref value);
        }

        throw new JsonException($"Expected 'ok' or 'error' but got '{reader.GetString()}'");
    }
}
