using System.Runtime.CompilerServices;
using Utilities.Monads;

namespace Utilities.Json;

internal readonly record struct ResultContainer<TOk, TError>(TOk? Ok, TError? Error, bool IsOk);

public sealed class ResultMonadJsonFactory : JsonConverterFactory {
	private ResultMonadJsonFactory() { }
	public static readonly ResultMonadJsonFactory Instance = new();

	public override bool CanConvert(Type t) {
		return t.IsGenericTypeDefinition is false && t.IsValueType && t.GetInterface("IResulMonad`2") is not null;
	}

	private static readonly Dictionary<Type, JsonConverter> Converters = new(8);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
		if (Converters.TryGetValue(typeToConvert, out var converter)) {
			return converter;
		}

		var i = typeToConvert.GetInterface("IResultMonad`2")!;
		var arg1 = i.GenericTypeArguments[0];
		var arg2 = i.GenericTypeArguments[1];

		var t = typeof(ResultJsonConverter<,,>).MakeGenericType(typeToConvert, arg1, arg2);
		var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
		Converters[typeToConvert] = converter = (JsonConverter)f.GetValue(null)!;
		return converter;
	}
}

internal sealed class ResultJsonConverter<TResult, TOk, TError> : JsonConverter<TResult>
	where TResult : struct, IResultMonad<TOk, TError>
	where TOk : notnull
	where TError : notnull {
	private ResultJsonConverter() { }
	public static readonly ResultJsonConverter<TResult, TOk, TError> Instance = new();

	public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options) {
		if (value.IsOk) {
			JsonSerializer.Serialize(writer, new ResultContainer<TOk, TError>(value.Ok, default, true), options);
		} else {
			JsonSerializer.Serialize(writer, new ResultContainer<TOk, TError>(default, value.Error, false), options);
		}
	}

	// WARNING. Works only if fields count of origin Result monad and generated are same
	// because of StructLayout(Auto). For each usage of generic struct type.
	// Compiler will create own memory type layout in relation to size of generic fields for ok,error values
	public override TResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var result = JsonSerializer.Deserialize<ResultContainer<TOk, TError>>(ref reader, options);
		if (result == default) {
			throw new();
		}

		if (result.IsOk) {
			var r = new Result<TOk, TError>(result.Ok!);
			return Unsafe.As<Result<TOk, TError>, TResult>(ref r);
		} else {
			var r = new Result<TOk, TError>(result.Error!);
			return Unsafe.As<Result<TOk, TError>, TResult>(ref r);
		}
	}
}
