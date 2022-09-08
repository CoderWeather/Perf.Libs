namespace Perf.SourceGeneration.Mediator.Diagnostics;

// Not working when messages declared in another assembly
// For global checking needs separate diagnostic analyzer

// There is no way to do cross-project analysis

// public static class MediatorDiagnostic {
// 	internal static readonly DiagnosticDescriptor MessageWithoutHandlerDescriptor = new(
// 		id: "MED1",
// 		title: "Missed message handler",
// 		messageFormat: "Message '{0}' is not used by any message handler",
// 		category: "Usage",
// 		defaultSeverity: DiagnosticSeverity.Warning,
// 		isEnabledByDefault: true
// 	);
//
// 	public static Diagnostic MessageWithoutHandler(TypeDeclarationSyntax node) =>
// 		Diagnostic.Create(MessageWithoutHandlerDescriptor, node.GetLocation(), node.Identifier.Text);
// }
