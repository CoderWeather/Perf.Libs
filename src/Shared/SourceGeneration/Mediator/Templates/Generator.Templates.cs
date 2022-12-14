namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
	public static void WriteMediatorClass(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
		var mt = assembly.Mediator;
		writer.WriteLine($"partial class {mt.Name}");
		using (NestedScope.Start(writer)) {
			writer.WriteLine(
				$"public {mt.Name}(IServiceProvider serviceProvider)"
			);
			using (NestedScope.Start(writer)) {
				writer.WriteLine("sp = serviceProvider;");
				if (assembly.AnyNotifications) {
					writer.WriteLine(
						$"this.notificationPublishChannel = sp.GetService<{GetNotificationPublishChannelName(assembly)}>()!;"
					);
				}
			}

			writer.WriteLine("private readonly IServiceProvider sp;");
			if (assembly.AnyNotifications) {
				writer.WriteLine($"private readonly {GetNotificationPublishChannelName(assembly)} notificationPublishChannel;");
			}

			WriteStaticHandlerCache(writer);

			WriteTypedMessageMethods(writer, handlers);
			var anyNotifications = handlers.Any(x => x.Common.Notifications.Any());
			WriteNotificationMethod(writer, assembly, anyNotifications);
			WriteHelperMethods(writer);
		}
	}

	public static void WriteMediatorInterfaceTypedMethods(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
		writer.WriteLine($"partial interface {assembly.MediatorInterface!.Name} {{");
		writer.Indent++;
		writer.WriteLine("#region Interface typed methods");
		foreach (var h in handlers) {
			foreach (var c in h.Common.Messages) {
				writer.WriteLine($"ValueTask<{c.Output}> Send({c.Input} request, CancellationToken ct = default);");
				if (assembly.IsScoped) {
					writer.WriteLine(
						$"ValueTask<{c.Output}> Send({c.InputTypeText} request, CancellationToken ct = default);"
					);
				}
			}

			foreach (var m in h.Mirror.Messages) {
				writer.WriteLine($"ValueTask<{m.Output}> Send({m.Input} request, CancellationToken ct = default);");
			}
		}

		writer.WriteLine("#endregion Interface typed methods");

		writer.Indent--;
		writer.WriteLine("}");
	}

	private static void WriteTypedMessageMethods(IndentedTextWriter writer, ImmutableArray<Handler> handlers) {
		writer.WriteLine("#region Send methods");
		foreach (var h in handlers) {
			foreach (var wc in h.Common.Messages) {
				writer.WriteLines(
					$"public ValueTask<{wc.Output}> Send({wc.Input} request, CancellationToken ct = default) {{",
					"	ArgumentNullException.ThrowIfNull(request);",
					$"	return WrapperCache<{wc.WrapperTypeText}>.GetOrCreate(sp).Handle(request, ct);",
					"}"
				);

				foreach (var cover in wc.Input.Interfaces.Where(x => x.FullPath() is "Utilities.Mediator.Messages.ICoveredBy")) {
					var by = cover.TypeArguments[0];
					writer.WriteLines(
						$"public ValueTask<{wc.Output}> Send({by} request, CancellationToken ct = default) {{",
						"	ArgumentNullException.ThrowIfNull(request);",
						$"	return WrapperCache<{wc.WrapperTypeText}>.GetOrCreate(sp).Handle(request.Map(), ct);",
						"}"
					);
				}
			}

			foreach (var wm in h.Mirror.Messages) {
				writer.WriteLines(
					$"public ValueTask<{wm.Output}> Send({wm.Input} request, CancellationToken ct = default) {{",
					"	ArgumentNullException.ThrowIfNull(request);",
					$"	return WrapperCache<{wm.WrapperTypeText}>.GetOrCreate(sp).Handle(request, ct);",
					"}"
				);

				foreach (var cover in wm.Input.Interfaces.Where(x => x.FullPath() is "Utilities.Mediator.Messages.ICoveredBy")) {
					var by = cover.TypeArguments[0];
					writer.WriteLines(
						$"public ValueTask<{wm.Output}> Send({by} request, CancellationToken ct = default) {{",
						"	ArgumentNullException.ThrowIfNull(request);",
						$"	return WrapperCache<{wm.WrapperTypeText}>.GetOrCreate(sp).Handle(request.Map(), ct);",
						"}"
					);
				}
			}
		}

		writer.WriteLine("#endregion Send methods");
	}

	private static void WriteNotificationMethod(IndentedTextWriter writer, Assembly assembly, bool anyHandlers) {
		writer.WriteLine(
			"public ValueTask Publish(INotification notification, CancellationToken ct = default)"
		);
		using (NestedScope.Start(writer)) {
			if (anyHandlers) {
				writer.WriteLines(
					"ArgumentNullException.ThrowIfNull(notification);",
					"return notificationPublishChannel.Publish(notification, ct);"
				);
			} else {
				writer.WriteLine("throw new MissingMessageHandlerException(notification);");
			}
		}
	}
}
