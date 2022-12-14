namespace Perf.SourceGeneration;

[Generator]
internal sealed class RecordAsDtoGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var types = context.SyntaxProvider
		   .CreateSyntaxProvider(SyntaxFilter, SyntaxTransform)
		   .Where(x => x is not null)
		   .Select((nts, ct) => nts!)
		   .Collect();

		context.RegisterSourceOutput(types, CodeGeneration);
	}

	private sealed record RecPack(
		INamedTypeSymbol Symbol,
		string DtoAccessibility,
		bool AsStruct,
		string NamePostfix
	) {
		public readonly List<(ITypeSymbol Type, string Name, ITypeSymbol? OriginalType)> Properties = new();
		public readonly List<(INamedTypeSymbol Type, object?[] Parameters)> Attributes = new();
		public readonly List<INamedTypeSymbol> Interfaces = new();

		public bool IsPartial { get; set; }

		public string DtoName => $"{Symbol.Name}{NamePostfix}";
	}

	private static bool SyntaxFilter(SyntaxNode node, CancellationToken ct) {
		if (node is not RecordDeclarationSyntax { RawKind: (int)SyntaxKind.RecordDeclaration } rec) {
			return false;
		}

		var attributeMarked = rec.AttributeLists
		   .Any(
				x => Enumerable.Any(
					x.Attributes,
					y => y.ToString() is { } s
					 && (s.StartsWith("RecordAsDtoAttribute") || s.StartsWith("RecordAsDto"))
				)
			);

		var isStruct = rec.Keyword.Text is "struct";

		var isAbstract = rec.Modifiers.Any(SyntaxKind.AbstractKeyword);

		return attributeMarked && isStruct is false && isAbstract is false;
	}

	private const string RecordAsDtoAttributeFullName = "ExpressMobile.Services.Shared.Utilities.SourceGeneration.RecordAsDtoAttribute";

	private const string AddAttributeToGeneratedDtoAttributeFullName =
		"ExpressMobile.Services.Shared.Utilities.SourceGeneration.AddAttributeToGeneratedDto";

	private const string AddInterfacesToGeneratedDtoAttributeFullName =
		"ExpressMobile.Services.Shared.Utilities.SourceGeneration.AddInterfacesToGeneratedDto";

	private static RecPack? SyntaxTransform(GeneratorSyntaxContext context, CancellationToken ct) {
		if (context.SemanticModel.GetDeclaredSymbol((RecordDeclarationSyntax)context.Node, ct) is not { } symbol) {
			return null;
		}

		var attributeCheck = symbol.TryGetAttribute(RecordAsDtoAttributeFullName);
		if (attributeCheck is null) {
			return null;
		}

		var accessibility = attributeCheck.TryGetArg<string>() ?? symbol.Accessibility();
		var asStruct = attributeCheck.TryGetArg<bool>();
		var namePostfix = attributeCheck.NamedArguments
			   .FirstOrDefault(x => x.Key is "NamePostfix")
			   .Value.Value as string
		 ?? "Dto";

		var isPartial = ((RecordDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PartialKeyword);

		var pack = new RecPack(symbol, accessibility, asStruct, namePostfix) {
			IsPartial = isPartial
		};

		var properties = symbol.GetMembers()
		   .OfType<IPropertySymbol>()
		   .Where(
				x => x is {
					IsIndexer: false,
					GetMethod: {
						MethodKind: MethodKind.PropertyGet,
						DeclaredAccessibility: Accessibility.Public
					},
					SetMethod: {
						MethodKind: MethodKind.PropertySet,
						DeclaredAccessibility: Accessibility.Public
					}
				}
			);

		foreach (var prop in properties) {
			var t = prop.Type;
			ITypeSymbol? originalType = null;

			switch (t) {
				case INamedTypeSymbol nt:
					if (nt.TryGetValueObjectKeyType() is { } ntVo) {
						originalType = t;
						t = ntVo;
					}

					break;
				case IArrayTypeSymbol at:
					if (at.ElementType is not INamedTypeSymbol et) {
						break;
					}

					if (et.TryGetValueObjectKeyType() is { } etVo) {
						originalType = t;
						t = context.SemanticModel.Compilation.CreateArrayTypeSymbol(etVo).WithNullableAnnotation(at.NullableAnnotation);
					}

					break;
			}

			pack.Properties.Add((t, prop.Name, originalType));
		}

		var addAttributesToDtoAttributes = symbol.GetAttributes()
		   .Where(x => x.AttributeClass?.FullPath().Equals(AddAttributeToGeneratedDtoAttributeFullName) is true);

		foreach (var attributeData in addAttributesToDtoAttributes) {
			var attributeParameters = attributeData.ConstructorArguments;
			var attrType = attributeParameters[0].As<INamedTypeSymbol>();
			if (attrType is null) {
				continue;
			}

			var attrArgs = attributeParameters[1];
			var attrValues = attrArgs.Values.Select(x => x.Value).ToArray();
			pack.Attributes.Add((attrType, attrValues));
		}

		var addInterfacesToDtoAttributes = symbol.GetAttributes()
		   .FirstOrDefault(x => x.AttributeClass?.FullPath().Equals(AddInterfacesToGeneratedDtoAttributeFullName) is true);

		if (addInterfacesToDtoAttributes is { ConstructorArguments.Length: > 0 }) {
			foreach (var tc in addInterfacesToDtoAttributes.ConstructorArguments[0].Values) {
				var t = tc.As<INamedTypeSymbol>();
				if (t is { TypeKind: TypeKind.Interface, IsGenericType: false } && t.GetMembers().Any() is false) {
					pack.Interfaces.Add(t);
				}
			}
		}

		return pack;
	}

	private static void CodeGeneration(SourceProductionContext context, ImmutableArray<RecPack> types) {
		if (types.IsDefaultOrEmpty) {
			return;
		}

		var typesGroupedByNamespace = types
		   .ToLookup(x => x.Symbol.ContainingNamespace, SymbolEqualityComparer.Default);

		foreach (var a in typesGroupedByNamespace) {
			var group = a.ToArray();
			var ns = (INamespaceSymbol)a.Key!;

			context.CancellationToken.ThrowIfCancellationRequested();
			var sourceCode = GenerateSourceCode(ns.ToDisplayString(), group)!;
			context.AddSource($"{ns.MinimalName()}.Dto.g.cs", SourceText.From(sourceCode, Encoding.UTF8));

			if (group.Any(x => x.IsPartial)) {
				context.CancellationToken.ThrowIfCancellationRequested();
				var partialsCode = GenerateRecordPartials(ns.ToDisplayString(), group)!;
				context.AddSource($"{ns.MinimalName()}.g.cs", SourceText.From(partialsCode, Encoding.UTF8));
			}
		}
	}

	private static string? GenerateSourceCode(string containingNamespace, RecPack[] types) {
		using var writer = new IndentedTextWriter(new StringWriter(), "	");

		writer.WriteLines(
			"// <auto-generated />",
			"#pragma warning disable CS8019",
			"#pragma warning disable CS0105",
			"#nullable enable",
			null,
			$"namespace {containingNamespace}.Dto;",
			null,
			"using System.Linq;",
			"using System.Linq.Expressions;",
			"using System.Runtime.CompilerServices;"
		);

		writer.WriteLines(
			"using ExpressMobile.Services.Shared.Utilities.SourceGeneration;",
			$"using {containingNamespace};",
			null
		);

		writer.WriteLine("public static class DtoExtensions");
		using (NestedScope.Start(writer)) {
			foreach (var t in types) {
				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static {t.Symbol.Name}Dto ToDto(this {t.Symbol.Name} rec)"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLines(
						$"var dto = new {t.Symbol.Name}Dto();",
						"dto.ApplyValues(rec);",
						"return dto;"
					);
				}

				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static IEnumerable<{t.Symbol.Name}Dto> SelectAsDto(this IEnumerable<{t.Symbol.Name}> enumerable)"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLine("return enumerable.Select(e => e.ToDto());");
				}

				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static IEnumerable<{t.Symbol.Name}> SelectAsEntity(this IEnumerable<{t.Symbol.Name}Dto> enumerable)"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLine("return enumerable.Select(dto => dto.ToEntity());");
				}

				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static IQueryable<{t.Symbol.Name}Dto> SelectAsDto(this IQueryable<{t.Symbol.Name}> query)"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLine($"return query.Select({t.Symbol.Name}Dto.EntityToDtoExpression);");
				}

				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static IQueryable<{t.Symbol.Name}> SelectAsEntity(this IQueryable<{t.Symbol.Name}Dto> query)"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLine($"return query.Select({t.Symbol.Name}Dto.DtoToEntityExpression);");
				}
			}
		}

		foreach (var t in types) {
			foreach (var a in t.Attributes) {
				var formattedParams = a.Parameters.Select(
					x => x switch {
						IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
						{ }            => x.ToString(),
						_              => "null"
					}
				);
				var @params = a.Parameters.Any()
					? $"({string.Join(",", formattedParams)})"
					: null;
				writer.WriteLine($"[{a.Type.GlobalName()}{@params}]");
			}

			string? additionalInterfaces = null;
			if (t.Interfaces.Any()) {
				additionalInterfaces = ", " + string.Join(", ", t.Interfaces.Select(x => x.GlobalName()));
			}

			var declarationType = t.AsStruct ? "struct" : "sealed class";
			writer.WriteLine($"{t.DtoAccessibility} {declarationType} {t.DtoName} : IGeneratedDto<{t.Symbol.GlobalName()}>{additionalInterfaces}");
			using (NestedScope.Start(writer)) {
				if (t.AsStruct) {
					writer.WriteLine($"public {t.DtoName}() {{ }}");
				}

				foreach (var (pt, pn, _) in t.Properties) {
					var defaultClosure = pt switch {
						IArrayTypeSymbol {
							NullableAnnotation: not NullableAnnotation.Annotated
						} at => $" = System.Array.Empty<{at.ElementType.GlobalName()}>();",
						{
							IsReferenceType: true,
							NullableAnnotation: not NullableAnnotation.Annotated
						} => " = null!;",
						{ IsReferenceType: true } => " = null!;",
						_                         => t.AsStruct ? " = default;" : null
					};

					writer.WriteLine($"public {pt.GlobalName()} {pn} {{ get; set; }}{defaultClosure}");
				}

				writer.WriteLineNoTabs(null!);

				writer.WriteLines(
					"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
					"public void Clear()"
				);
				using (NestedScope.Start(writer)) {
					foreach (var (pt, pn, _) in t.Properties) {
						var defaultClosure = pt switch {
							IArrayTypeSymbol {
								NullableAnnotation: not NullableAnnotation.Annotated
							} at => $"System.Array.Empty<{at.ElementType.GlobalName()}>()",
							{
								IsReferenceType: true,
								NullableAnnotation: NullableAnnotation.Annotated
							} => "null",
							{ IsReferenceType: true } => "null!",
							_                         => "default"
						};
						writer.WriteLine($"this.{pn} = {defaultClosure};");
					}
				}

				writer.WriteLines(
					"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
					$"public void ApplyValues({t.Symbol.Name} rec)"
				);
				using (NestedScope.Start(writer)) {
					foreach (var (pt, pn, ot) in t.Properties) {
						if (ot is not null) {
							if (ot.TypeKind is TypeKind.Array) {
								var pta = (IArrayTypeSymbol)pt;
								var ota = (IArrayTypeSymbol)ot;
								var nullable = pta.ElementType is {
									IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated
								};
								var nullWord = nullable ? "null" : "null!";
								writer.WriteLine(
									$"this.{pn} = rec.{pn} != {nullWord} ? ExpressMobile.Services.Shared.Utilities.ArrayUnsafe.MapCast<{ota.ElementType.GlobalName()}, {pta.ElementType.GlobalName()}>(rec.{pn}) : {nullWord};"
								);
							} else {
								writer.WriteLine($"this.{pn} = ({pt.GlobalName()})rec.{pn};");
							}
						} else {
							writer.WriteLine($"this.{pn} = rec.{pn};");
						}
					}
				}

				writer.WriteLines(
					"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
					$"{t.Symbol.Accessibility()} {t.Symbol.Name} ToEntity()"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLine($"return new {t.Symbol.Name}(");
					writer.Indent++;

					var withComma = t.Properties.Count - 1;
					foreach (var (pt, pn, ot) in t.Properties) {
						if (ot is not null) {
							if (ot.TypeKind is TypeKind.Array) {
								var pta = (IArrayTypeSymbol)pt;
								var ota = (IArrayTypeSymbol)ot;
								var nullable = pta.ElementType is {
									IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated
								};
								var nullWord = nullable ? "null" : "null!";
								writer.WriteLine(
									$"{pn}: this.{pn} != {nullWord} ? ExpressMobile.Services.Shared.Utilities.ArrayUnsafe.MapCast<{pta.ElementType.GlobalName()}, {ota.ElementType.GlobalName()}>(this.{pn}) : {nullWord}{(withComma-- > 0 ? ',' : null)}"
								);
							} else {
								writer.WriteLine(
									$"{pn}: ({ot.GlobalName()})this.{pn}{(withComma-- > 0 ? ',' : null)}"
								);
							}
						} else {
							writer.WriteLine($"{pn}: this.{pn}{(withComma-- > 0 ? ',' : null)}");
						}
					}

					writer.Indent--;
					writer.WriteLine(");");
				}

				// DTO -> Entity
				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static readonly Expression<Func<{t.DtoName},{t.Symbol.Name}>> DtoToEntityExpression = dto => new("
				);
				writer.Indent++;
				{
					var withComma = t.Properties.Count - 1;
					foreach (var (_, pn, ot) in t.Properties) {
						if (ot is not null) {
							if (ot.TypeKind is TypeKind.Array) {
								var ota = (IArrayTypeSymbol)ot;
								var nullable = ota.ElementType is {
									IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated
								};
								var nullWord = nullable ? "null" : "null!";
								writer.WriteLine(
									$"dto.{pn} != {nullWord} ? dto.{pn}.Cast<{ota.ElementType.GlobalName()}>().ToArray() : {nullWord}{(withComma-- > 0 ? ',' : null)}"
								);
							} else {
								writer.WriteLine(
									$"({ot.GlobalName()})dto.{pn}{(withComma-- > 0 ? ',' : null)}"
								);
							}
						} else {
							writer.WriteLine($"dto.{pn}{(withComma-- > 0 ? ',' : null)}");
						}
					}
				}
				writer.Indent--;
				writer.WriteLine(");");

				// Entity -> DTO
				writer.WriteLine(
					$"{t.Symbol.Accessibility()} static readonly Expression<Func<{t.Symbol.Name},{t.DtoName}>> EntityToDtoExpression = e => new() {{"
				);
				writer.Indent++;
				{
					var withComma = t.Properties.Count - 1;
					foreach (var (pt, pn, ot) in t.Properties) {
						if (ot is not null) {
							if (ot.TypeKind is TypeKind.Array) {
								var pta = (IArrayTypeSymbol)pt;
								var nullable = pta.ElementType is {
									IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated
								};
								var nullWord = nullable ? "null" : "null!";
								writer.WriteLine(
									$"{pn} = e.{pn} != {nullWord} ?e.{pn}.Cast<{pta.ElementType.GlobalName()}>().ToArray() : {nullWord}{(withComma-- > 0 ? "," : null)}"
								);
							} else {
								writer.WriteLine(
									$"{pn} = ({ot.GlobalName()})e.{pn}{(withComma-- > 0 ? "," : null)}"
								);
							}
						} else {
							writer.WriteLine($"{pn} = e.{pn}{(withComma-- > 0 ? "," : null)}");
						}
					}
				}
				writer.Indent--;
				writer.WriteLine("};");
			}
		}

		var resultCode = writer.InnerWriter.ToString();
		return resultCode;
	}

	private static string? GenerateRecordPartials(string containingNamespace, RecPack[] types) {
		using var writer = new IndentedTextWriter(new StringWriter(), "	");

		writer.WriteLines(
			"// <auto-generated />",
			"#pragma warning disable CS8019",
			"#pragma warning disable CS0105",
			"#nullable enable",
			null,
			$"namespace {containingNamespace};",
			null,
			"using ExpressMobile.Services.Shared.Utilities.SourceGeneration;",
			null
		);

		foreach (var t in types) {
			if (t.IsPartial) {
				writer.WriteLine($"{t.Symbol.Accessibility()} partial record {t.Symbol.Name} : IHaveGeneratedDto<Dto.{t.DtoName}>;");
			}
		}

		var sourceCode = writer.InnerWriter.ToString();
		return sourceCode;
	}
}
