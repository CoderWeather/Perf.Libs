using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Utilities.Json;
using Utilities.MsgPack;

namespace Utilities;

public static class GlobalOptions {
	static GlobalOptions() {
		MessagePack = MessagePackSerializerOptions.Standard
		   .WithResolver(OwnStaticResolver.Instance)
		   .WithCompression(MessagePackCompression.None);
		OwnStaticResolver.Instance.AddResolvers(UpdateMessagePackResolver.Instance);
		OwnStaticResolver.Instance.AddResolvers(ResultMonadResolver.Instance);
		OwnStaticResolver.Instance.AddResolvers(ValueObjectMessagePackResolver.Instance);
		OwnStaticResolver.Instance.AddResolvers(BasicFilterFormatterResolver.Instance);
		MessagePackSerializer.DefaultOptions = MessagePack;
		JsonWeb = new(JsonSerializerDefaults.Web);
		JsonWeb.Converters.Add(UpdateJsonFactory.Instance);
		JsonWeb.Converters.Add(ResultMonadJsonFactory.Instance);
		JsonWeb.Converters.Add(ValueObjectJsonConverterFactory.Instance);
		JsonWeb.Converters.Add(BasicFilterJsonFactory.Instance);
		Json = new(JsonSerializerDefaults.General);
		Json.Converters.Add(UpdateJsonFactory.Instance);
		Json.Converters.Add(ResultMonadJsonFactory.Instance);
		Json.Converters.Add(ValueObjectJsonConverterFactory.Instance);
		Json.Converters.Add(BasicFilterJsonFactory.Instance);
	}

	public static readonly MessagePackSerializerOptions MessagePack;

	public static readonly JsonSerializerOptions JsonWeb;
	public static readonly JsonSerializerOptions Json;

	public static void AddMessagePackFormatter(IMessagePackFormatter formatter) {
		OwnStaticResolver.Instance.AddFormatters(formatter);
	}

	public static void AddMessagePackResolver(IFormatterResolver resolver) {
		OwnStaticResolver.Instance.AddResolvers(resolver);
	}
}

public sealed class OwnStaticResolver : IFormatterResolver {
	public static readonly OwnStaticResolver Instance = new() {
		resolversList = {
			StandardResolverAllowPrivate.Instance
		}
	};

	private OwnStaticResolver() { }
	private bool freezed;

	private readonly List<IMessagePackFormatter> formattersList = new();
	private readonly List<IFormatterResolver> resolversList = new();

	public void WithFormatters(params IMessagePackFormatter[] ar) {
		if (freezed) {
			throw new InvalidOperationException($"{nameof(OwnStaticResolver)} is freezed");
		}

		formattersList.Clear();
		formattersList.AddRange(ar);
	}

	public void WithResolvers(params IFormatterResolver[] ar) {
		if (freezed) {
			throw new InvalidOperationException($"{nameof(OwnStaticResolver)} is freezed");
		}

		resolversList.Clear();
		resolversList.AddRange(ar);
	}

	public void AddFormatters(params IMessagePackFormatter[] ar) {
		if (freezed) {
			throw new InvalidOperationException($"{nameof(OwnStaticResolver)} is freezed");
		}

		var e = ar.Where(x => formattersList.Any(y => y.GetType() == x.GetType()) is false);
		formattersList.AddRange(e);
	}

	public void AddResolvers(params IFormatterResolver[] ar) {
		if (freezed) {
			throw new InvalidOperationException($"{nameof(OwnStaticResolver)} is freezed");
		}

		var e = ar.Where(x => resolversList.Any(y => y.GetType() == x.GetType()) is false);
		resolversList.AddRange(e);
	}

	private IMessagePackFormatter[]? formatters;
	private IFormatterResolver[]? resolvers;

	private IMessagePackFormatter[] Formatters => formatters ??= formattersList.ToArray();

	private IFormatterResolver[] Resolvers => resolvers ??= resolversList.ToArray();

	public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

	private static class Cache<T> {
		public static readonly IMessagePackFormatter<T>? Formatter;

		static Cache() {
			Instance.freezed = true;
			foreach (var item in Instance.Formatters) {
				if (item is IMessagePackFormatter<T> f) {
					Formatter = f;
					return;
				}
			}

			foreach (var item in Instance.Resolvers) {
				var f = item.GetFormatter<T>();
				if (f != null) {
					Formatter = f;
					return;
				}
			}
		}
	}
}
