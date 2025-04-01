// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Serialization.SystemTextJson;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class MultiResultJsonConverterFactory : JsonConverterFactory {
    public static readonly MultiResultJsonConverterFactory Instance = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericTypeDefinition is false
        && typeToConvert.IsValueType
        && typeToConvert.GetInterface("IMultiResultHolder") is { IsConstructedGenericType: true, GenericTypeArguments.Length: >= 2 and <= 8 };

    static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (Converters.TryGetValue(typeToConvert, out var converter)) {
            return converter;
        }

        // var t = typeof(MultiResultJsonConverter<>).MakeGenericType(typeToConvert);
        // var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        // Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
        return converter!;
    }
}

// TODO
#if NET7_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
sealed class A { };
