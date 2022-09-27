namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
	private static void WriteStaticHandlerCache(IndentedTextWriter writer) {
		writer.WriteLines(
			"private readonly record struct CacheEntry<T>(T Value) where T : class;"
		);
		writer.WriteLine("private static class WrapperCache<T> where T : class");
		using (NestedScope.Start(writer)) {
			writer.WriteLines(
				"public static CacheEntry<T> entry;",
				"public static T GetOrCreate(IServiceProvider sp) => entry.Value ?? (entry = new(sp.GetService<T>()!)).Value;"
			);
		}
	}

	private static void WriteHelperMethods(IndentedTextWriter writer) {
		writer.WriteLines(
			// "[System.Diagnostics.CodeAnalysis.DoesNotReturn]",
			"private static void ThrowInvalidMessage(object message) => throw new MissingMessageHandlerException(message);"
		);

		writer.WriteLines(
			// "[System.Diagnostics.CodeAnalysis.DoesNotReturn]",
			"private static void ThrowArgumentNullOrInvalidMessage(object? message, string paramName)"
		);
		using (NestedScope.Start(writer)) {
			writer.WriteLines(
				"if (message is null) ArgumentNullException.ThrowIfNull(message, paramName);",
				"else ThrowInvalidMessage(message);"
			);
		}
	}
}
