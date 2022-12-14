using MessagePack;
using MessagePack.Formatters;
using Utilities.Generic;

namespace Utilities.MsgPack;

public sealed class UpdateMessagePackResolver : IFormatterResolver {
	private UpdateMessagePackResolver() { }
	public static readonly UpdateMessagePackResolver Instance = new();

	public IMessagePackFormatter<T>? GetFormatter<T>() {
		var t = Typeof<T>();

		if (t.Name is "Update`1" && t.IsConstructedGenericType) {
			var argType = t.GetGenericArguments()[0];
			if (argType.GetConstructor(Type.EmptyTypes) is not null && argType.IsGenericTypeDefinition is false) {
				return Cache<T>.Formatter;
			}
		}

		return null;
	}

	private static class Cache<T> {
		public static readonly IMessagePackFormatter<T>? Formatter;

		static Cache() {
			var updateModelType = Typeof<T>();
			var t = updateModelType.GetGenericArguments()[0];

			var msgPack = typeof(UpdateMessagePackFormatter<>).MakeGenericType(t)
			   .GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
			Formatter = msgPack.GetValue(null) as IMessagePackFormatter<T>;
		}
	}
}

public sealed class UpdateMessagePackFormatter<T> : IMessagePackFormatter<Update<T>?> where T : notnull, new() {
	private UpdateMessagePackFormatter() { }
	public static readonly UpdateMessagePackFormatter<T> Instance = new();

	public void Serialize(ref MessagePackWriter writer, Update<T>? value, MessagePackSerializerOptions options) {
		if (value is null) {
			writer.WriteNil();
			return;
		}

		MessagePackSerializer.Serialize(ref writer, value.SerializableValues, options);
	}

	public Update<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
		if (reader.TryReadNil()) {
			return null;
		}

		var dict = MessagePackSerializer.Deserialize<Dictionary<string, (string, object?, bool)>>(ref reader, options);
		return new(dict);
	}
}
