namespace Perf.SourceGeneration.Mapster;

[Generator]
internal sealed class MapperExtensionGenerator : IIncrementalGenerator {
	private sealed class TypesTupleEqualityComparer : IEqualityComparer<(ITypeSymbol Left, ITypeSymbol Right)> {
		private static readonly SymbolEqualityComparer Basic = SymbolEqualityComparer.Default;
		private TypesTupleEqualityComparer() { }
		public static readonly TypesTupleEqualityComparer Instance = new();

		public bool Equals((ITypeSymbol Left, ITypeSymbol Right) x, (ITypeSymbol Left, ITypeSymbol Right) y) {
			return Basic.Equals(x.Left, x.Left) && Basic.Equals(x.Right, y.Right);
		}

		public int GetHashCode((ITypeSymbol Left, ITypeSymbol Right) obj) => Basic.GetHashCode(obj.Left) + Basic.GetHashCode(obj.Right);
	}

	private readonly record struct Mapper(INamedTypeSymbol Config) {
		public readonly HashSet<(ITypeSymbol From, ITypeSymbol To)> TypesPairs = new(TypesTupleEqualityComparer.Instance);
		public readonly HashSet<(ITypeSymbol From, ITypeSymbol To)> AdditionalMappings = new(TypesTupleEqualityComparer.Instance);
		public readonly HashSet<INamedTypeSymbol> ValueObjects = new(SymbolEqualityComparer.Default);
	}

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var configs = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, ct) => node is ClassDeclarationSyntax {
						TypeParameterList: null,
						BaseList.Types.Count: 1
					} cd
				 && cd.Modifiers.Any(SyntaxKind.SealedKeyword)
				 && cd.Modifiers.Any(SyntaxKind.AbstractKeyword) is false
				 && cd.BaseList.Types[0].Type is SimpleNameSyntax { Identifier.Text: "TypeAdapterConfig" },
				SyntaxTransform
			)
		   .Where(x => x != default);

		context.RegisterSourceOutput(configs.Collect(), CodeGeneration);
	}

	private const string RecordAsDtoAttribute = "Services.Shared.Abstractions.Attributes.RecordAsDtoAttribute";
	private const string CopyTypeMembersAttribute = "Utilities.SourceGeneration.CopyTypeMembersAttribute";

	private const string IgnoreValueObjectsAttribute = "Utilities.SourceGeneration.IgnoreValueObjects";
	private const string IgnoreRecordAsDtoAttribute = "Utilities.SourceGeneration.IgnoreRecordAsDto";
	private const string IgnoreCopyTypeMembersAttribute = "Utilities.SourceGeneration.IgnoreCopyTypeMembers";

	private static Mapper SyntaxTransform(GeneratorSyntaxContext context, CancellationToken ct = default) {
		var typeAdapterConfigType = context.TryGetType("Mapster.TypeAdapterConfig");

		if (typeAdapterConfigType is null) {
			return default;
		}

		var newConfigMethod = typeAdapterConfigType.GetMembers()
		   .OfType<IMethodSymbol>()
		   .FirstOrDefault(
				x => x is {
					Name: "NewConfig",
					IsGenericMethod: true,
					TypeParameters.Length: 2
				}
			)!;

		var syntax = (ClassDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, ct);
		if (symbol is null) {
			return default;
		}

		if (symbol.BaseType?.FullPath() is "Mapster.TypeAdapterConfig" is false || symbol.InstanceConstructors.Length is not 1) {
			return default;
		}

		var configConstructor = symbol.InstanceConstructors[0];
		if (configConstructor.DeclaringSyntaxReferences.IsDefaultOrEmpty) {
			return default;
		}

		var configEntries = configConstructor.DeclaringSyntaxReferences[0]
		   .GetSyntax(ct)
		   .ChildNodes()
		   .OfType<BlockSyntax>()
		   .FirstOrDefault()
		  ?.ChildNodes()
		   .OfType<ExpressionStatementSyntax>()
		   .Select(x => x.Expression)
		   .OfType<InvocationExpressionSyntax>()
		   .Select(x => x.Expression)
		   .ToArray();

		if (configEntries is null) {
			return default;
		}

		var ignoreValueObjects = symbol.HasAttribute(IgnoreValueObjectsAttribute);
		var ignoreRecordAsDto = symbol.HasAttribute(IgnoreRecordAsDtoAttribute);
		var ignoreCopyTypeMembers = symbol.HasAttribute(IgnoreCopyTypeMembersAttribute);

		var pack = new Mapper(symbol);
		var configsList = new List<IMethodSymbol>();

		foreach (var ce in configEntries) {
			var checkSymbol = context.SemanticModel.GetSymbolInfo(ce, ct).Symbol;

			if (checkSymbol is IMethodSymbol { TypeArguments.Length: 2 } ims) {
				configsList.Add(ims);
				continue;
			}

			var expr = ce;
			InvocationExpressionSyntax? invocationExpr = null;
			while (expr is MemberAccessExpressionSyntax {
					   Expression: InvocationExpressionSyntax ies
				   } maes) {
				if (maes.GetHashCode() == maes.Expression.GetHashCode()) {
					break;
				}

				expr = ies.Expression;
				invocationExpr = ies;
			}

			if (invocationExpr is not null) {
				checkSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr, ct).Symbol;
				if (checkSymbol is IMethodSymbol { TypeArguments.Length: 2 } ms) {
					configsList.Add(ms);
				}
			}
		}

		foreach (var m in configsList) {
			if (m.OriginalDefinition.StrictEquals(newConfigMethod) is false) {
				continue;
			}

			var fromType = m.TypeArguments[0];
			var toType = m.TypeArguments[1];

			if (fromType is { IsValueType: false, IsReferenceType: false } || toType is { IsValueType: false, IsReferenceType: false }) {
				continue;
			}

			if (fromType.NullableAnnotation is NullableAnnotation.Annotated) {
				fromType = fromType.WithNullableAnnotation(NullableAnnotation.None).OriginalDefinition;
			}

			if (toType.NullableAnnotation is NullableAnnotation.Annotated) {
				toType = toType.WithNullableAnnotation(NullableAnnotation.None).OriginalDefinition;
			}

			if (pack.TypesPairs.Any(x => x.From.StrictEquals(fromType))) {
				continue;
			}

			if (ignoreValueObjects is false) {
				if (fromType is INamedTypeSymbol nts1) {
					CheckTypeForValueObjects(nts1);
				}

				if (toType is INamedTypeSymbol nts2) {
					CheckTypeForValueObjects(nts2);
				}
			}

			void CheckTypeForValueObjects(INamedTypeSymbol type) {
				var members = type.GetMembers();

				foreach (var p in members.OfType<IPropertySymbol>()) {
					if (Check(p.Type) is { } t) {
						pack.ValueObjects.Add(t);
					}
				}

				foreach (var f in members.OfType<IFieldSymbol>()) {
					if (Check(f.Type) is { } t) {
						pack.ValueObjects.Add(t);
					}
				}

				static INamedTypeSymbol? Check(ITypeSymbol t) {
					if (t is IArrayTypeSymbol ats) {
						return Check(ats.ElementType);
					}

					if (t.IsValueNullable()) {
						var nt = (INamedTypeSymbol)t;
						t = nt.TypeArguments[0];
					}

					return t.IsValueObject() ? t as INamedTypeSymbol : null;
				}
			}

			if (ignoreRecordAsDto is false) {
				if (fromType.HasAttribute(RecordAsDtoAttribute)) {
					var dtoTypeName = $"{fromType.ContainingNamespace.ToDisplayString()}.Dto.{fromType.Name}Dto";
					var dtoType = context.TryGetType(dtoTypeName);
					if (dtoType is not null) {
						pack.AdditionalMappings.Add((dtoType, toType));
					}
				}

				if (toType.HasAttribute(RecordAsDtoAttribute)) {
					var dtoTypeName = $"{toType.ContainingNamespace.ToDisplayString()}.Dto.{toType.Name}Dto";
					var dtoType = context.TryGetType(dtoTypeName);
					if (dtoType is not null) {
						pack.AdditionalMappings.Add((fromType, dtoType));
					}
				}
			}

			if (ignoreCopyTypeMembers is false) {
				if (fromType.TryGetAttribute(CopyTypeMembersAttribute) is { } fromCopy) {
					var origin = fromCopy.ConstructorArguments[0].As<INamedTypeSymbol>()!;
					pack.AdditionalMappings.Add((fromType, origin));
					// pack.AdditionalMappings.Add((origin, fromType));
				}

				if (toType.TryGetAttribute(CopyTypeMembersAttribute) is { } toCopy) {
					var origin = toCopy.ConstructorArguments[0].As<INamedTypeSymbol>()!;
					pack.AdditionalMappings.Add((toType, origin));
					// pack.AdditionalMappings.Add((origin, toType));
				}
			}

			pack.TypesPairs.Add((fromType, toType));
		}

		return pack;
	}

	private static void CodeGeneration(SourceProductionContext context, ImmutableArray<Mapper> types) {
		if (types.IsDefaultOrEmpty) {
			return;
		}

		var grouped = types.ToLookup(x => x.Config.ContainingNamespace, SymbolEqualityComparer.Default);

		context.CancellationToken.ThrowIfCancellationRequested();
		foreach (var group in grouped) {
			var mappers = group.ToArray();
			var ns = (INamespaceSymbol)group.Key!;
			var sourceCode = ProcessMappers(ns, mappers);

			var encodedSourceCode = SourceText.From(sourceCode, Encoding.UTF8);

			context.CancellationToken.ThrowIfCancellationRequested();
			context.AddSource($"{ns.MinimalName()}.g.cs", encodedSourceCode);
		}
	}

	private static string ProcessMappers(INamespaceSymbol ns, Mapper[] mappers) {
		using var writer = new IndentedTextWriter(new StringWriter(), "	");

		writer.WriteLines(
			"// <auto-generated />",
			"#pragma warning disable CS8019",
			"#pragma warning disable CS0105",
			"#nullable enable",
			null,
			$"namespace {ns};",
			null,
			"using Mapster;",
			"using System.Collections.Generic;",
			null
		);

		var nsFromTypes = mappers
		   .SelectMany(x => x.TypesPairs)
		   .SelectMany(x => new[] { x.Item1, x.Item2 })
		   .Select(x => x.ContainingNamespace.ToDisplayString())
		   .Append(ns.ToDisplayString())
		   .Distinct();

		foreach (var n in nsFromTypes) {
			writer.WriteLine($"using {n};");
		}

		writer.WriteLine();

		foreach (var mapper in mappers) {
			writer.WriteLine(
				$"{mapper.Config.Accessibility()} static class {mapper.Config.Name.TrimStart('I')}_ExtensionMethods"
			);
			using (NestedScope.Start(writer)) {
				writer.WriteLineNoTabs("#region Typed Extension Methods");
				var te = mapper.TypesPairs.Concat(mapper.AdditionalMappings)
				   .Select(x => x.From)
				   .DistinctBy(x => x.OriginalDefinition, SymbolEqualityComparer.Default);
				foreach (var fromType in te) {
					WriteMethod(fromType.GlobalName());

					if (fromType is not IArrayTypeSymbol or { Name: not "List" }) {
						WriteMethod($"{fromType}[]");
						WriteMethod($"List<{fromType}>");
					}

					void WriteMethod(string parameterType) {
						writer.WriteLines(
							$"public static T MapTo<T>(this {parameterType} source) where T : notnull {{",
							"	ArgumentNullException.ThrowIfNull(source);",
							$"	return Cache<{parameterType}, T>.Map(Instance, source);",
							"}"
						);
					}
				}

				writer.WriteLineNoTabs("#endregion");

				writer.WriteLine($"private static readonly {mapper.Config.GlobalName()} Instance = new();");
				writer.WriteLine("private static class Cache<TFrom, TTo>");
				using (NestedScope.Start(writer)) {
					writer.WriteLines(
						"private static Func<TFrom, TTo>? lambda;",
						$"public static TTo Map({mapper.Config.GlobalName()} config, TFrom source) =>",
						"	(lambda ??= config.GetMapFunction<TFrom, TTo>()).Invoke(source);"
					);
				}

				writer.WriteLine($"static {mapper.Config.Name.TrimStart('I')}_ExtensionMethods()");
				using (NestedScope.Start(writer)) {
					WriteValueObjectsConfigs(writer, mapper);
					WriteMappingForGeneratedDto(writer, mapper);

					writer.WriteLine("Instance.Compile();");
				}

				writer.WriteLines(
					"[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]",
					"public static MapWrapper<T> Map<T>(this T value) where T : class => new(value);",
					"public readonly record struct MapWrapper<SourceType>(SourceType Value) where SourceType : class {",
					"	public TargetType To<TargetType>() => Cache<SourceType, TargetType>.Map(Instance, Value);",
					"}"
				);
			}
		}

		return writer.InnerWriter.ToString()!;
	}

	private static void WriteValueObjectsConfigs(IndentedTextWriter writer, Mapper mapper) {
		var voToWrite = new List<(INamedTypeSymbol VoType, INamedTypeSymbol InnerType)>();

		foreach (var vo in mapper.ValueObjects) {
			var innerType = vo.TryGetValueObjectKeyType();
			if (innerType is null) {
				continue;
			}

			voToWrite.Add((vo, innerType));
		}

		if (voToWrite.Count is 0) {
			return;
		}

		foreach (var (vo, inner) in voToWrite) {
			writer.WriteLines(
				$"Instance.NewConfig<{vo.GlobalName()}, {inner.GlobalName()}>().MapWith(x => ({inner.GlobalName()})x);",
				$"Instance.NewConfig<{vo.GlobalName()}?, {inner.GlobalName()}?>().MapWith(x => ({inner.GlobalName()}?)(x ?? default));",
				$"Instance.NewConfig<{vo.GlobalName()}, {inner.GlobalName()}?>().MapWith(x => ({inner.GlobalName()}?)x);",
				$"Instance.NewConfig<{inner.GlobalName()}, {vo.GlobalName()}>().MapWith(x => ({vo.GlobalName()})x);",
				$"Instance.NewConfig<{inner.GlobalName()}?, {vo.GlobalName()}?>().MapWith(x => ({vo.GlobalName()}?)(x ?? default));"
			);
		}
	}

	private static void WriteMappingForGeneratedDto(IndentedTextWriter writer, Mapper mapper) {
		foreach (var (fromType, toType) in mapper.AdditionalMappings) {
			if (toType.IsRecord) {
				writer.WriteLine($"Instance.NewConfig<{fromType}, {toType}>().MapToConstructor(true);");
			} else {
				writer.WriteLine($"Instance.NewConfig<{fromType}, {toType}>();");
			}
		}
	}
}
