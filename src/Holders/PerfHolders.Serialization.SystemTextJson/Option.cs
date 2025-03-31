// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Serialization.SystemTextJson;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Internal;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class OptionHolderJsonConverterFactory : JsonConverterFactory {
    public static readonly OptionHolderJsonConverterFactory Instance = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericTypeDefinition is false
        && typeToConvert.IsValueType
        && typeToConvert.GetInterface("IOptionHolder`1") is not null;

    static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (Converters.TryGetValue(typeToConvert, out var converter)) {
            return converter;
        }

        var i = typeToConvert.GetInterface("IOptionHolder`1")!;
        var arg1 = i.GenericTypeArguments[0];

        var t = typeof(OptionHolderJsonConverter<,>).MakeGenericType(typeToConvert, arg1);
        var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
        return converter;
    }
}

#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class OptionHolderJsonConverter<TOption, TValue> : JsonConverter<TOption>
    where TOption : struct, IOptionHolder<TValue>
    where TValue : notnull {
    public static readonly OptionHolderJsonConverter<TOption, TValue> Instance = new();

    public override void Write(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options) {
        if (value.IsSome) {
            JsonSerializer.Serialize(writer, value.Some, options);
        } else {
            writer.WriteNullValue();
        }
    }

    public override TOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is JsonTokenType.Null) {
            reader.Read();
            return default;
        }

        var value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        var o = DynamicCast.Cast<TValue, TOption>(ref value);
        return o;
    }
}
