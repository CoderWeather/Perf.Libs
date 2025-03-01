namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
    private static string GetNotificationBusName(Assembly assembly) => $"{assembly.Mediator.Name}_NotificationBus";

    private static string GetNotificationPublishChannelName(Assembly assembly) => $"I{assembly.Mediator.Name}_NotificationPublishChannel";

    public static void WriteNotificationBus(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
        writer.WriteLines(
            $"{assembly.Mediator.Accessibility()} interface {GetNotificationPublishChannelName(assembly)} {{",
            "	ValueTask Publish(INotification notification, CancellationToken ct = default);",
            "}"
        );

        writer.WriteLines(
            $"{assembly.Mediator.Accessibility()} sealed class {GetNotificationBusName(assembly)} : BackgroundService, {GetNotificationPublishChannelName(assembly)}"
        );
        using (NestedScope.Start(writer)) {
            writer.WriteLines(
                $"public {GetNotificationBusName(assembly)}(IServiceProvider serviceProvider) {{",
                "	sp = serviceProvider;",
                $"	this.logger = sp.GetService<ILogger<{GetNotificationBusName(assembly)}>>();",
                "}"
            );

            writer.WriteLines(
                "private readonly IServiceProvider sp;",
                $"private readonly ILogger<{GetNotificationBusName(assembly)}>? logger;",
                "private readonly Channel<INotification> channel = Channel.CreateUnbounded<INotification>(new() {",
                "	AllowSynchronousContinuations = true,",
                "	SingleReader = true,",
                "	SingleWriter = true",
                "});"
            );
            writer.WriteLines(
                "private static class HandlerCache<T> where T : class {",
                "	private static T? Instance;",
                "	public static T Get(IServiceProvider sp) => Instance ??= sp.GetService<T>()!;",
                "}"
            );

            writer.WriteLines(
                "public ValueTask Publish(INotification notification, CancellationToken ct) => channel.Writer.WriteAsync(notification, ct);"
            );

            writer.WriteLine("protected override async Task ExecuteAsync(CancellationToken ct)");
            using (NestedScope.Start(writer)) {
                var notifications = handlers
                   .SelectMany(x => x.Common.Notifications
                                  .Select(y => new {
                                           Handler = x,
                                           Notification = y
                                       }
                                   )
                    )
                   .ToArray();
                if (notifications.Any()) {
                    writer.WriteLine(
                        "await foreach (var notification in channel.Reader.ReadAllAsync(ct))"
                    );
                    using (NestedScope.Start(writer)) {
                        writer.WriteLine("try");
                        using (NestedScope.Start(writer)) {
                            writer.WriteLine("switch (notification)");
                            using (NestedScope.Start(writer)) {
                                foreach (var n in notifications) {
                                    var h = n.Handler;
                                    var nc = n.Notification;
                                    writer.WriteLine($"case {nc.Input} n:");
                                    using (NestedScope.Start(writer)) {
                                        writer.WriteLines(
                                            $"var handler = HandlerCache<{h.Type}>.Get(sp);",
                                            "await handler.Handle(n, ct);",
                                            "break;"
                                        );
                                    }
                                }

                                writer.WriteLine("default:");
                                using (NestedScope.Start(writer)) {
                                    writer.WriteLines(
                                        "if (notification == null!) ArgumentNullException.ThrowIfNull(notification);",
                                        "else throw new MissingMessageHandlerException(notification);",
                                        "break;"
                                    );
                                }
                            }
                        }

                        writer.WriteLine("catch (Exception ex)");
                        using (NestedScope.Start(writer)) {
                            writer.WriteLine(
                                "logger?.LogError(ex, \"Error publishing notification: {Notification}\", notification);"
                            );
                        }
                    }
                } else {
                    writer.WriteLines(
                        "logger?.LogInformation(\"No notification handlers registered\");",
                        "await Task.CompletedTask;"
                    );
                }
            }
        }
    }
}
