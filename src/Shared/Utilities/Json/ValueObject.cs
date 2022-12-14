using System.Runtime.CompilerServices;
using Perf.ValueObjects;

namespace Utilities.Json;

public sealed class ValueObjectJsonConverterFactory : JsonConverterFactory {
	private ValueObjectJsonConverterFactory() { }
	public static readonly ValueObjectJsonConverterFactory Instance = new();

	public override bool CanConvert(Type typeToConvert) {
		return typeToConvert.IsGenericTypeDefinition is false && typeToConvert.GetInterface("IValueObject`1") is not null;
	}

	private static readonly Dictionary<Type, JsonConverter?> Cache = new();

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
		if (Cache.TryGetValue(typeToConvert, out var converter)) {
			return converter;
		}

		var voType = typeToConvert;
		var keyType = typeToConvert.GetInterface("IValueObject`1")!.GenericTypeArguments[0];
		var converterType = typeof(ValueObjectJsonConverter<,>).MakeGenericType(voType, keyType);
		var f = converterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
		Cache[typeToConvert] = converter = f.GetValue(null) as JsonConverter;
		return converter;
	}
}

internal sealed class ValueObjectJsonConverter<T, TKey> : JsonConverter<T> where T : struct, IValueObject<TKey> {
	private ValueObjectJsonConverter() { }
	public static readonly JsonConverter Instance = new ValueObjectJsonConverter<T, TKey>();

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
		JsonSerializer.Serialize(writer, value, options);
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var key = JsonSerializer.Deserialize<TKey>(ref reader, options);
		if (key is null) {
			throw new InvalidCastException();
		}

		var vo = Unsafe.As<TKey, T>(ref key);
		return vo;
	}
}
