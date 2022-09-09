namespace Perf.SourceGeneration.Internal;

public static class DiagnosticExtensions {
    public static void Warning(this SourceProductionContext context, string message, CancellationToken ct) {
        context.ReportDiagnostic(Diagnostic.Create(
            new(
                "VC0001",
                "Warning Message",
                "Message: {0}",
                "Source Generation",
                DiagnosticSeverity.Warning,
                true
            ),
            null,
            message
        ));
    }
}