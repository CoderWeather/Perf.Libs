namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
    public static void WriteRegistrationExtension(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
        writer.WriteLine(
            $"{assembly.Mediator.Accessibility()} static class {assembly.Mediator.Name}_RegistrationExtensions");
        using (NestedScope.Start(writer)) {
            writer.WriteLine(
                $"public static void Register{assembly.Mediator.Name}(this IServiceCollection services)");
            using (NestedScope.Start(writer)) {
                var mediatorInterfaceName =
                    assembly.MediatorInterface?.GlobalName() ?? "IMediator";
                writer.WriteLines(
                    $"services.AddSingleton<{assembly.Mediator}>();",
                    $"services.AddSingleton<{mediatorInterfaceName}>(sp => sp.GetService<{assembly.Mediator}>()!);",
                    $"services.AddSingleton<{GetNotificationPublishChannelName(assembly)}, {GetNotificationBusName(assembly)}>();",
                    $"services.AddHostedService(sp => ({GetNotificationBusName(assembly)})sp.GetService<{GetNotificationPublishChannelName(assembly)}>()!);"
                );

                foreach (var h in handlers) {
                    writer.WriteLines($"services.TryAddSingleton<{h.Type.GlobalName()}>();");
                    foreach (var c in h.Common.Messages) {
                        writer.WriteLines(
                            $"services.AddSingleton(sp => new {c.WrapperTypeText}(",
                            $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                            $"		sp.GetServices<{c.PipelineTypeText}>()));"
                        );
                    }

                    foreach (var m in h.Mirror.Messages) {
                        writer.WriteLines(
                            $"services.AddSingleton(sp => new {m.WrapperTypeText}(",
                            $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                            $"		sp.GetService<{m.OriginWrapperTypeText}>()!.Handle,",
                            $"		sp.GetServices<{m.PipelineTypeText}>()));"
                        );
                    }
                }
            }
        }
    }

    public static void WriteScopeRegistrationExtension(IndentedTextWriter writer,
        Assembly assembly,
        ImmutableArray<Handler> handlers
    ) {
        writer.WriteLine(
            $"{assembly.Mediator.Accessibility()} static class {assembly.Mediator.Name}_RegistrationExtensions");
        using (NestedScope.Start(writer)) {
            writer.WriteLine(
                $"public static void Register{assembly.Mediator.Name}(this IServiceCollection services)");
            using (NestedScope.Start(writer)) {
                var mediatorInterfaceName = assembly.MediatorInterface?.GlobalName() ?? "IScopedMediator";
                writer.WriteLines(
                    "services.AddScopedMediatorCore();",
                    $"services.AddScoped<{assembly.Mediator}>();",
                    $"services.AddScoped<{mediatorInterfaceName}>(sp => sp.GetService<{assembly.Mediator}>()!);"
                );

                foreach (var h in handlers) {
                    writer.WriteLines($"services.TryAddSingleton<{h.Type.GlobalName()}>();");
                    foreach (var c in h.Common.Messages) {
                        writer.WriteLines(
                            $"services.AddSingleton(sp => new {c.WrapperTypeText}(",
                            $"		sp.GetService<{h.Type.GlobalName()}>()!,",
                            $"		sp.GetServices<{c.PipelineTypeText}>()));"
                        );
                    }
                }
            }
        }
    }
}