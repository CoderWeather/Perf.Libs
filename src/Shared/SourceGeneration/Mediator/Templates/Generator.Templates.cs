namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
	public static void WriteMediatorClass(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
		var mt = assembly.Mediator;
		writer.WriteLine($"partial class {mt.Name} : IMediator");
		using (NestedScope.Start(writer)) {
			writer.WriteLine(
				$"public {mt.Name}(IServiceProvider serviceProvider, {GetNotificationPublishChannelName(assembly)} notificationPublishChannel)");
			using (NestedScope.Start(writer)) {
				writer.WriteLines(
					"sp = serviceProvider;",
					"this.notificationPublishChannel = notificationPublishChannel;"
				);
			}

			writer.WriteLines(
				"private readonly IServiceProvider sp;",
				$"private readonly {GetNotificationPublishChannelName(assembly)} notificationPublishChannel;"
			);

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

		writer.Indent--;
		writer.WriteLine("}");
	}

	private static void WriteTypedMessageMethods(IndentedTextWriter writer, ImmutableArray<Handler> handlers) {
		foreach (var h in handlers) {
			foreach (var wc in h.Common.Messages) {
				writer.WriteLines(
					$"public ValueTask<{wc.Output}> Send({wc.Input} request, CancellationToken ct = default) {{",
					"	ArgumentNullException.ThrowIfNull(request);",
					$"	return WrapperCache<{wc.WrapperTypeText}>.GetOrCreate(sp).Handle(request, ct);",
					"}"
				);
			}

			foreach (var wm in h.Mirror.Messages) {
				writer.WriteLines(
					$"public ValueTask<{wm.Output}> Send({wm.Input} request, CancellationToken ct = default) {{",
					"	ArgumentNullException.ThrowIfNull(request);",
					$"	return WrapperCache<{wm.WrapperTypeText}>.GetOrCreate(sp).Handle(request, ct);",
					"}"
				);
			}
		}
	}

	private static void WriteNotificationMethod(IndentedTextWriter writer, Assembly assembly, bool anyHandlers) {
		writer.WriteLine(
			"public ValueTask Publish(INotification notification, CancellationToken ct = default)");
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
