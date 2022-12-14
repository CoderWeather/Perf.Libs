using Utilities.Generic;

namespace Utilities.Json;

public sealed class UpdateJsonFactory : JsonConverterFactory {
	private UpdateJsonFactory() { }
	public static readonly UpdateJsonFactory Instance = new();

	// JsonConverter

	public override bool CanConvert(Type t) {
		if (t.Name is "Update`1" && t.IsConstructedGenericType) {
			var argType = t.GetGenericArguments()[0];
			if (argType.GetConstructor(Type.EmptyTypes) is not null && argType.IsGenericTypeDefinition is false) {
				return true;
			}
		}

		return false;
	}

	private static readonly Dictionary<Type, JsonConverter?> JsonConverters = new();

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
		if (JsonConverters.TryGetValue(typeToConvert, out var converter)) {
			return converter;
		}

		var t = typeToConvert.GetGenericArguments()[0];
		var json = typeof(UpdateJsonConverter<>).MakeGenericType(t)
		   .GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
		JsonConverters[typeToConvert] = converter = json.GetValue(null) as JsonConverter;
		return converter;
	}
}

internal sealed class UpdateJsonConverter<T> : JsonConverter<Update<T>?> where T : notnull, new() {
	private UpdateJsonConverter() { }
	public static readonly UpdateJsonConverter<T> Instance = new();

	public override void Write(Utf8JsonWriter writer, Update<T>? value, JsonSerializerOptions options) {
		if (value is null) {
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value.SerializableValues, options);
	}

	public override Update<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		if (reader.TokenType is JsonTokenType.Null) {
			return null;
		}

		var dict = JsonSerializer.Deserialize<Dictionary<string, (string, object?, bool)>>(ref reader, options);
		return dict is not null ? new(dict) : null;
	}
}
