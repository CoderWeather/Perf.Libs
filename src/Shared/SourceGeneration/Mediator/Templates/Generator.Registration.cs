namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
    public static void WriteRegistrationExtension(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
        writer.WriteLine(
            $"{assembly.Mediator.Accessibility()} static class {assembly.Mediator.Name}_RegistrationExtensions"
        );
        using (NestedScope.Start(writer)) {
            writer.WriteLine(
                $"public static void Register{assembly.Mediator.Name}(this IServiceCollection services)"
            );
            using (NestedScope.Start(writer)) {
                var mediatorInterfaceName =
                    assembly.MediatorInterface?.GlobalName() ?? "IMediator";
                writer.WriteLines(
                    $"services.AddSingleton<{assembly.Mediator}>();",
                    $"services.AddSingleton<{mediatorInterfaceName}>(sp => sp.GetService<{assembly.Mediator}>()!);"
                );
                if (assembly.AnyNotifications) {
                    writer.WriteLines(
                        $"services.AddSingleton<{GetNotificationPublishChannelName(assembly)}, {GetNotificationBusName(assembly)}>();",
                        $"services.AddHostedService(sp => ({GetNotificationBusName(assembly)})sp.GetService<{GetNotificationPublishChannelName(assembly)}>()!);"
                    );
                }

                writer.WriteLine("#region Handlers registration");
                foreach (var h in handlers) {
                    writer.WriteLines($"services.TryAddSingleton<{h.Type.GlobalName()}>();");
                    foreach (var c in h.Common.Messages) {
                        writer.WriteLines(
                            $"services.TryAddSingleton(sp => new {c.WrapperTypeText}(",
                            $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                            $"		sp.GetServices<{c.PipelineTypeText}>()));"
                        );
                    }

                    foreach (var m in h.Mirror.Messages) {
                        var originHandler = handlers.AsSpan()
                           .Find(x => x.Common.Messages.Exists(wm => wm.Input.StrictEquals(m.OriginInput)));
                        if (originHandler != default) {
                            writer.WriteLines(
                                $"services.TryAddSingleton(sp => new {m.WrapperTypeText}(",
                                $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                                $"		sp.GetService<{originHandler.Type.GlobalName()}>()!.Handle,",
                                $"		sp.GetServices<{m.PipelineTypeText}>()));"
                            );
                        }
                    }
                }

                writer.WriteLine("#endregion Handlers registration");
            }
        }
    }

    public static void WriteScopeRegistrationExtension(
        IndentedTextWriter writer,
        Assembly assembly,
        ImmutableArray<Handler> handlers
    ) {
        writer.WriteLine(
            $"{assembly.Mediator.Accessibility()} static class {assembly.Mediator.Name}_RegistrationExtensions"
        );
        using (NestedScope.Start(writer)) {
            writer.WriteLine(
                $"public static void Register{assembly.Mediator.Name}(this IServiceCollection services)"
            );
            using (NestedScope.Start(writer)) {
                var mediatorInterfaceName = assembly.MediatorInterface?.GlobalName() ?? "IScopedMediator";
                writer.WriteLines(
                    "services.AddScopedMediatorCore();",
                    $"services.AddScoped<{assembly.Mediator}>();",
                    $"services.AddScoped<{mediatorInterfaceName}>(sp => sp.GetService<{assembly.Mediator}>()!);"
                );

                writer.WriteLine("#region Handlers registration");
                foreach (var h in handlers) {
                    writer.WriteLines($"services.TryAddSingleton<{h.Type.GlobalName()}>();");
                    foreach (var c in h.Common.Messages) {
                        writer.WriteLines(
                            $"services.TryAddSingleton(sp => new {c.WrapperTypeText}(",
                            $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                            $"		sp.GetServices<{c.PipelineTypeText}>()));"
                        );
                    }
                }

                writer.WriteLine("#endregion Handlers registration");
            }
        }
    }
}
