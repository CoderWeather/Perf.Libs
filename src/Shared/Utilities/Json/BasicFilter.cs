using Utilities.Generic;

namespace Utilities.Json;

public sealed class BasicFilterJsonFactory : JsonConverterFactory {
    private BasicFilterJsonFactory() { }
    public static readonly BasicFilterJsonFactory Instance = new();

    public override bool CanConvert(Type t) {
        if (t.Name is "BasicFilter`1" && t.IsConstructedGenericType) {
            var arg = t.GetGenericArguments()[0];
            if (arg.IsGenericTypeDefinition is false) {
                return true;
            }
        }

        return false;
    }

    private static readonly Dictionary<Type, JsonConverter?> Converters = new();

    public override JsonConverter? CreateConverter(Type t, JsonSerializerOptions options) {
        if (Converters.TryGetValue(t, out var converter)) {
            return converter;
        }

        var arg = t.GetGenericArguments()[0];
        var json = typeof(BasicFilterJsonConverter<>).MakeGenericType(arg);
        var field = json.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
        Converters[t] = converter = field.GetValue(null) as JsonConverter;
        return converter;
    }
}

internal sealed class BasicFilterJsonConverter<T> : JsonConverter<BasicFilter<T>?> where T : notnull {
    private BasicFilterJsonConverter() { }
    public static readonly BasicFilterJsonConverter<T> Instance = new();

    public override void Write(Utf8JsonWriter writer, BasicFilter<T>? value, JsonSerializerOptions options) {
        if (value is null) {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value.Entries, options);
    }

    public override BasicFilter<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var entries = JsonSerializer.Deserialize<FilterEntry[]>(ref reader, options);
        return entries is null ? null : new(entries);
    }
}
