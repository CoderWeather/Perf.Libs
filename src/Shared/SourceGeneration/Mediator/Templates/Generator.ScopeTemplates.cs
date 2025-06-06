namespace Perf.SourceGeneration.Mediator.Templates;

internal static partial class Generator {
    public static void WriteScopedMediatorClass(IndentedTextWriter writer, Assembly assembly, ImmutableArray<Handler> handlers) {
        var mt = assembly.Mediator;
        writer.WriteLine($"partial class {mt.Name}");
        using (NestedScope.Start(writer)) {
            writer.WriteLines(
                $"public {mt.Name}(IServiceProvider serviceProvider, IScopeRegistry registry) {{",
                "	sp = serviceProvider;",
                "	this.registry = registry;",
                "}"
            );

            writer.WriteLines(
                "private readonly IServiceProvider sp;",
                "private readonly IScopeRegistry registry;",
                "public Guid CorrelationId { get; private set; } = GuidPerf.New();",
                "private bool shouldDispose = true;"
            );

            writer.WriteLines(
                "public bool RegisterEntry<TEntry>(TEntry entry) where TEntry : notnull {",
                "	return registry.RegisterEntry(CorrelationId, entry);",
                "}"
            );

            writer.WriteLines(
                "public void AttachToScope(Guid correlationId) {",
                "	if (shouldDispose) {",
                "		registry.DropEntry(CorrelationId);",
                "	}",
                "	CorrelationId = correlationId;",
                "	shouldDispose = false;",
                "}"
            );

            writer.WriteLines("public void ReleaseScope() => registry.DropEntry(CorrelationId);");

            writer.WriteLines(
                "public Guid RecreateScope() {",
                "	if (shouldDispose) {",
                "		registry.DropEntry(CorrelationId);",
                "	}",
                "	return CorrelationId = GuidPerf.New();",
                "}"
            );

            writer.WriteLines(
                "public void Dispose() {",
                "	if (shouldDispose) {",
                "		registry.DropEntry(CorrelationId);",
                "		shouldDispose = false;",
                "	}",
                "}"
            );

            writer.WriteLines(
                "public ValueTask DisposeAsync() {",
                "	Dispose();",
                "	return ValueTask.CompletedTask;",
                "}"
            );

            WriteStaticHandlerCache(writer);
            WriteTypedScopedMessageMethods(writer, handlers);
            WriteHelperMethods(writer);
        }
    }

    private static void WriteTypedScopedMessageMethods(IndentedTextWriter writer, ImmutableArray<Handler> handlers) {
        writer.WriteLine("#region Send methods");
        foreach (var h in handlers) {
            foreach (var c in h.Common.Messages) {
                writer.WriteLines(
                    $"public ValueTask<{c.Output}> Send({c.Input} request, CancellationToken ct = default) {{",
                    "	ArgumentNullException.ThrowIfNull(request);",
                    $"	var sr = new {c.InputTypeText}(request, CorrelationId, sp);",
                    $"	return WrapperCache<{c.WrapperTypeText}>.GetOrCreate(sp).Handle(sr, ct);",
                    "}"
                );

                if (c.Input.Interfaces.FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.ICoveredBy") is { } cover) {
                    var by = cover.TypeArguments[0];
                    writer.WriteLines(
                        $"public ValueTask<{c.Output}> Send({by} request, CancellationToken ct = default) {{",
                        "	ArgumentNullException.ThrowIfNull(request);",
                        $"	var sr = new {c.InputTypeText}(request.Map(), CorrelationId, sp);",
                        $"	return WrapperCache<{c.WrapperTypeText}>.GetOrCreate(sp).Handle(sr, ct);",
                        "}"
                    );
                }
            }
        }

        writer.WriteLine("#endregion Send methods");
    }
}
