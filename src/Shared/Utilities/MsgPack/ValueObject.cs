using System.Reflection;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Formatters;

namespace Utilities.MsgPack;

public sealed class ValueObjectMessagePackResolver : IFormatterResolver {
	private ValueObjectMessagePackResolver() { }
	public static readonly ValueObjectMessagePackResolver Instance = new();

	public IMessagePackFormatter<T>? GetFormatter<T>() {
		var t = typeof(T);
		if (t.IsGenericTypeDefinition is false && t.GetInterface("IValueObject`1") is not null) {
			return Cache<T>.Formatter;
		}

		return null;
	}

	private static class Cache<T> {
		public static readonly IMessagePackFormatter<T>? Formatter;

		static Cache() {
			var i = typeof(T).GetInterface("IValueObject`1")!;
			var arg = i.GenericTypeArguments[0];
			var formatterType = typeof(ValueObjectMessagePackFormatter<,>).MakeGenericType(typeof(T), arg);
			var field = formatterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
			var formatter = field.GetValue(null) as IMessagePackFormatter<T>;
			Formatter = formatter;
		}
	}
}

internal sealed class ValueObjectMessagePackFormatter<T, TKey> : IMessagePackFormatter<T> where T : struct, IValueObject<TKey> {
	private ValueObjectMessagePackFormatter() { }
	public static readonly IMessagePackFormatter Instance = new ValueObjectMessagePackFormatter<T, TKey>();

	public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options) {
		MessagePackSerializer.Serialize(ref writer, value, options);
	}

	public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
		var key = MessagePackSerializer.Deserialize<TKey>(ref reader, options);
		if (key is null) {
			throw new InvalidCastException();
		}

		var vo = Unsafe.As<TKey, T>(ref key);
		return vo;
	}
}
