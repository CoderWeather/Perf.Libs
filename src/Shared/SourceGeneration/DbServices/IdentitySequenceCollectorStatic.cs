namespace Perf.SourceGeneration.DbServices;

[Generator]
public sealed class IdentitySequenceCollectorStatic : IIncrementalGenerator {
	private const string BaseCollectorGlobalName =
		"global::Services.Shared.Infrastructure.DbServices.LongIdentitySequenceBasedCollector";

	private const string AttributeMarkerFullName = "Services.Shared.Infrastructure.DbServices.GenerateStaticCollectorForIdentity";

	private readonly record struct Wrapper(INamedTypeSymbol Collector) {
		public readonly List<(string Name, AttributeData, INamedTypeSymbol? Type)> Identities = new();
	}

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var collectors = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, ct) => {
					if (node is ClassDeclarationSyntax {
							AttributeLists.Count: > 0,
							TypeParameterList: null, BaseList: null
						} c) {
						if (c.Modifiers.Any(SyntaxKind.StaticKeyword) && c.Modifiers.Any(SyntaxKind.PartialKeyword)) {
							foreach (var al in c.AttributeLists) {
								foreach (var a in al.Attributes) {
									if (a.Name.ToString() is "GenerateStaticCollectorForIdentity") {
										return true;
									}
								}
							}
						}
					}

					return false;
				},
				static (context, ct) => {
					var syntax = (ClassDeclarationSyntax)context.Node;
					if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
						return default;
					}

					var collector = new Wrapper(symbol);
					foreach (var ad in symbol.GetAttributes()) {
						if (ad.AttributeClass is null || ad.AttributeClass.FullPath() is not AttributeMarkerFullName) {
							continue;
						}

						var firstArg = ad.ConstructorArguments[0];
						var type = firstArg.As<INamedTypeSymbol>();
						var identityName = type?.Name ?? firstArg.As<string>()!;
						collector.Identities.Add((identityName, ad, type));
					}

					return collector;
				}
			)
		   .Where(x => x != default && x.Identities.Any());

		context.RegisterSourceOutput(
			collectors,
			static (context, wrapper) => {
				var assembly = wrapper.Collector.ContainingAssembly;
				var rootNamespace = assembly.Name;

				using var writer = new IndentedTextWriter(new StringWriter(), "	");
				writer.WriteLines("// <auto-generated />", null, $"namespace {rootNamespace};");

				writer.WriteLine($"partial class {wrapper.Collector.Name} {{");
				writer.Indent++;

				foreach (var (id, _, t) in wrapper.Identities) {
					writer.WriteLine($"private static {BaseCollectorGlobalName} {id}_Collector;");
					if (t is not null) {
						writer.WriteLine($"public static {t.GlobalName()} Get{id}() => ({t.GlobalName()}){id}_Collector.Get();");
					} else {
						writer.WriteLine($"public static long Get{id}() => {id}_Collector.Get();");
					}
				}

				{
					writer.WriteLine(
						$"public static void Initialize{wrapper.Collector.Name}(this Microsoft.AspNetCore.Builder.WebApplication app) {{"
					);
					writer.Indent++;

					foreach (var (id, ad, _) in wrapper.Identities) {
						string? additionalParameters = null;
						var queueSize = ad.ConstructorArguments[1].As<int>();
						var schema = ad.ConstructorArguments[2].As<string>();
						if (queueSize != default) {
							additionalParameters += $", queueSize: {queueSize}";
						}

						if (schema != default) {
							additionalParameters += $", schema: \"{schema}\"";
						}

						writer.WriteLine($"{id}_Collector = new(app, \"{id}\"{additionalParameters});");
					}

					writer.Indent--;
					writer.WriteLine("}");
				}

				writer.Indent--;
				writer.WriteLine("}");

				var sourceCode = writer.InnerWriter.ToString()!;
				var sourceText = SourceText.From(sourceCode, Encoding.UTF8);
				context.AddSource("IdCollector.g.cs", sourceText);
			}
		);
	}
}
