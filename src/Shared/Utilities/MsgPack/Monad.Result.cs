using System.Reflection;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Formatters;
using Utilities.Monads;

namespace Utilities.MsgPack;

internal readonly record struct ResultContainer<TOk, TError>(TOk? Ok, TError? Error, bool IsOk);

public sealed class ResultMonadResolver : IFormatterResolver {
	private ResultMonadResolver() { }
	public static readonly ResultMonadResolver Instance = new();

	public IMessagePackFormatter<T>? GetFormatter<T>() {
		var t = typeof(T);

		if (t.IsGenericTypeDefinition is false && t.IsValueType && t.GetInterface("IResulMonad`2") is not null) {
			return Cache<T>.Formatter;
		}

		return null;
	}

	private static class Cache<T> {
		public static readonly IMessagePackFormatter<T>? Formatter;

		static Cache() {
			var t = typeof(T);
			var i = t.GetInterface("IResultMonad`2")!;
			var okType = i.GenericTypeArguments[0];
			var erType = i.GenericTypeArguments[1];
			var formatterType = typeof(ResultFormatter<,,>).MakeGenericType(t, okType, erType);
			var field = formatterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
			Formatter = field.GetValue(null) as IMessagePackFormatter<T>;
		}
	}
}

internal sealed class ResultFormatter<TResult, TOk, TError> : IMessagePackFormatter<TResult>
	where TResult : struct, IResultMonad<TOk, TError>
	where TOk : notnull
	where TError : notnull {
	private ResultFormatter() { }
	public static readonly ResultFormatter<TResult, TOk, TError> Instance = new();

	public void Serialize(ref MessagePackWriter writer, TResult value, MessagePackSerializerOptions options) {
		if (value.IsOk) {
			MessagePackSerializer.Serialize(ref writer, new ResultContainer<TOk, TError>(value.Ok, default, true), options);
		} else {
			MessagePackSerializer.Serialize(ref writer, new ResultContainer<TOk, TError>(default, value.Error, false), options);
		}
	}

	public TResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
		var result = MessagePackSerializer.Deserialize<ResultContainer<TOk, TError>>(ref reader, options);
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
