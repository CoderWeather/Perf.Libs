namespace Perf.SourceGeneration.Utilities;

public static class DiagnosticExtensions {
	public static void Warning(this SourceProductionContext context, string message) {
		context.ReportDiagnostic(
			Diagnostic.Create(
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
			)
		);
	}
}
