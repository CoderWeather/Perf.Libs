namespace Perf.Monads.Result;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Result<int, Uri>))]
public sealed partial class TestJsonContext : JsonSerializerContext;

public sealed class MonadResultJsonConverterFactory : JsonConverterFactory {
    public static readonly MonadResultJsonConverterFactory Instance = new();

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsGenericTypeDefinition is false
         && typeToConvert.IsValueType
         && typeToConvert.GetInterface("IResultMonad`2") is not null;
    }

    private static readonly Dictionary<Type, JsonConverter> Converters = new();

#if NET7_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (Converters.TryGetValue(typeToConvert, out var converter)) {
            return converter;
        }

        var i = typeToConvert.GetInterface("IResultMonad`2")!;
        var arg1 = i.GenericTypeArguments[0];
        var arg2 = i.GenericTypeArguments[1];

        var t = typeof(MonadResultJsonConverter<,,>).MakeGenericType(typeToConvert, arg1, arg2);
        var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
        return converter;
    }
}

sealed class MonadResultJsonConverter<TResult, TOk, TError> : JsonConverter<TResult>
    where TResult : struct, IResultMonad<TOk, TError>
    where TOk : notnull
    where TError : notnull {
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
            throw new JsonException($"Expected {JsonTokenType.StartObject} at {reader.Position.GetInteger()} but got '{reader.TokenType}'");
        }
        reader.Read();

        if (reader.ValueSpan.SequenceEqual("ok"u8)) {
            var value = JsonSerializer.Deserialize<TOk>(ref reader, options)!;
            reader.Read();
            return Unsafe.As<TOk, TResult>(ref value);
            // return (TResult)value;
        }
        if (reader.ValueSpan.SequenceEqual("error"u8)) {
            var value = JsonSerializer.Deserialize<TError>(ref reader, options)!;
            reader.Read();
            return Unsafe.As<TError, TResult>(ref value);
            // return value;
        }

        throw new JsonException($"Expected 'ok' or 'error' at {reader.Position.GetInteger()} but got '{reader.GetString()}'");
    }
}

public sealed class MonadResultJsonConverter<TOk, TError> : JsonConverter<Result<TOk, TError>>
    where TOk : notnull
    where TError : notnull {
    private MonadResultJsonConverter() { }
    public static readonly MonadResultJsonConverter<TOk, TError> Instance = new();

    public override void Write(Utf8JsonWriter writer, Result<TOk, TError> value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        if (value.IsOk) {
            writer.WritePropertyName("ok"u8);
            JsonSerializer.Serialize(writer, value.Ok, options);
        } else {
            writer.WritePropertyName("error"u8);
            JsonSerializer.Serialize(writer, value.Error, options);
        }
        writer.WriteEndObject();
    }

    public override Result<TOk, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is not JsonTokenType.StartObject) {
            throw new JsonException($"Expected {JsonTokenType.StartObject} at {reader.Position.GetInteger()} but got '{reader.TokenType}'");
        }
        reader.Read();

        if (reader.ValueSpan.SequenceEqual("ok"u8)) {
            var value = JsonSerializer.Deserialize<TOk>(ref reader, options)!;
            reader.Read();
            return value;
        }
        if (reader.ValueSpan.SequenceEqual("error"u8)) {
            var value = JsonSerializer.Deserialize<TError>(ref reader, options)!;
            reader.Read();
            return value;
        }

        throw new JsonException($"Expected 'ok' or 'error' at {reader.Position.GetInteger()} but got '{reader.GetString()}'");
    }
}
