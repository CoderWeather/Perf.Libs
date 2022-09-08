namespace Perf.SourceGeneration.EF;

[Generator]
internal sealed class PostgresEnumMappingGenerator : IIncrementalGenerator {
	private sealed record ContextPack(INamedTypeSymbol Symbol) {
		public List<EnumPack> Enums { get; } = new();
	}

	private sealed record EnumPack(INamedTypeSymbol Symbol, string? Schema, string? Name);

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var types = context.SyntaxProvider
		   .CreateSyntaxProvider(SyntaxFilter, SyntaxTransform)
		   .Where(x => x is not null)
		   .Select((nts, ct) => nts!)
		   .Collect();

		context.RegisterSourceOutput(types, CodeGeneration);
	}

	private static bool SyntaxFilter(SyntaxNode node, CancellationToken ct) {
		if (node is ClassDeclarationSyntax cls) {
			var attributeCheck = cls.AttributeLists
			   .Any(x => x.Attributes
				   .Any(y => y.Name.ToString() == "AutoGenPostgresEnumMapping"));
			var havePartialKeyword = cls.Modifiers.Any(SyntaxKind.PartialKeyword);
			var haveAbstractKeyword = cls.Modifiers.Any(SyntaxKind.AbstractKeyword);
			return attributeCheck && havePartialKeyword && haveAbstractKeyword is false;
		}

		return false;
	}

	private static ContextPack? SyntaxTransform(GeneratorSyntaxContext context, CancellationToken ct) {
		var attributeType = context.TryGetType("Perf.Infrastructure.EF.SourceGeneration.AutoGenPostgresEnumMapping"
		);
		var dbContextType = context.TryGetType("Microsoft.EntityFrameworkCore.DbContext"
		);

		if (attributeType is null || dbContextType is null) {
			return null;
		}

		var symbol = context.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)context.Node, ct);
		if (symbol is null or { IsStatic: true }) {
			return null;
		}

		var baseTypeCheck = symbol.BaseType is { } bt
		 && (bt.IsDefinition ? bt : bt.OriginalDefinition).StrictEquals(dbContextType);
		if (baseTypeCheck is false) {
			return null;
		}

		var pack = new ContextPack(symbol);

		string? globalSchemaName = null;
		var globalSchemaField = symbol.GetMembers()
		   .OfType<IFieldSymbol>()
		   .FirstOrDefault(f => f is {
				IsConst: true,
				Name: "Schema",
				HasConstantValue: true
			});
		if (globalSchemaField is not null) {
			globalSchemaName = globalSchemaField.ConstantValue as string;
		}

		var attributes = symbol.GetAttributes()
		   .Where(x => x.AttributeClass?.StrictEquals(attributeType) ?? false)
		   .ToArray();

		foreach (var attr in attributes) {
			var enumType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
			var schema = globalSchemaName ?? attr.ConstructorArguments[1].Value as string;
			var name = attr.ConstructorArguments[2].Value as string;

			if (enumType is null || enumType.IsEnum() is false) {
				continue;
			}

			pack.Enums.Add(new(enumType, schema, name));
		}

		return pack.Enums.Any() ? pack : null;
	}

	private static void CodeGeneration(SourceProductionContext context, ImmutableArray<ContextPack> types) {
		if (types.IsDefaultOrEmpty) {
			return;
		}

		foreach (var type in types) {
			var sourceCode = GenerateSourceCode(type);
			if (sourceCode is null) {
				continue;
			}

			context.CancellationToken.ThrowIfCancellationRequested();
			context.AddSource($"{type.Symbol.Name}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
		}
	}

	private static string? GenerateSourceCode(ContextPack context) {
		using var writer = new IndentedTextWriter(new StringWriter(), "	");

		writer.WriteLines(
			"// <auto-generated />",
			"using Npgsql;",
			"using Microsoft.EntityFrameworkCore;"
		);

		var nsToImport = context.Enums
		   .Select(x => x.Symbol.ContainingNamespace.ToString()!)
		   .Distinct()
		   .OrderBy(x => x);

		foreach (var ns in nsToImport) {
			writer.WriteLine($"using {ns};");
		}

		writer.WriteLines(
			null,
			$"namespace {context.Symbol.ContainingNamespace};",
			null
		);

		writer.WriteLines(
			$"partial class {context.Symbol.Name} {{",
			"	private static void MapEnums(ModelBuilder modelBuilder) {"
		);
		writer.Indent += 2;

		if (context.Enums.Any(x => x.Name is null)) {
			writer.WriteLine("var typeNameTranslator = NpgsqlConnection.GlobalTypeMapper.DefaultNameTranslator;");
		}

		foreach (var type in context.Enums) {
			using (NestedScope.Start(writer)) {
				var schemaPrefix = type.Schema is not null ? $"{type.Schema}." : null;
				var schemaParameterValue = type.Schema is not null ? $"\"{type.Schema}\"" : "null";
				if (type.Name is null) {
					writer.WriteLines(
						$"var translatedName = typeNameTranslator.TranslateTypeName(typeof({type.Symbol.Name}).Name);",
						$"NpgsqlConnection.GlobalTypeMapper.MapEnum<{type.Symbol.Name}>($\"{schemaPrefix}{{translatedName}}\");",
						$"modelBuilder.HasPostgresEnum<{type.Symbol.Name}>({schemaParameterValue}, translatedName);"
					);
				} else {
					writer.WriteLines(
						$"NpgsqlConnection.GlobalTypeMapper.MapEnum<{type.Symbol.Name}>(\"{schemaPrefix}{type.Name}\");",
						$"modelBuilder.HasPostgresEnum<{type.Symbol.Name}>({schemaParameterValue}, \"{type.Name}\");"
					);
				}
			}
		}

		writer.Indent -= 2;
		writer.WriteLines(
			"	}",
			"}"
		);

		var resultCode = writer.InnerWriter.ToString();
		return resultCode;
	}
}
