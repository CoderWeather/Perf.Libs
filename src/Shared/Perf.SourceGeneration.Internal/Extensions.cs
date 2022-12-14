namespace Perf.SourceGeneration.Utilities;

internal static class Extensions {
	public static void WriteLines(this IndentedTextWriter writer, params string?[] strings) {
		foreach (var s in strings) {
			if (s is null) {
				writer.WriteLineNoTabs("");
			} else {
				writer.WriteLine(s);
			}
		}
	}

	public static void WriteLines<T>(this IndentedTextWriter writer, T strings)
		where T : IEnumerable<string> {
		foreach (var s in strings) {
			writer.WriteLine(s);
		}
	}

	public static string GlobalName(this ITypeSymbol type) {
		var nullablePostfix = type.NullableAnnotation is NullableAnnotation.Annotated && type.IsValueNullable() is false
			? "?"
			: null;
		return $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{nullablePostfix}";
	}

	private static readonly SymbolDisplayFormat FullPathFormat = new(
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.None
	);

	public static string FullPath(this ITypeSymbol type) => type.ToDisplayString(FullPathFormat);

	public static string MinimalName(this ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

	public static bool HaveRecordAsDtoAttribute(this ITypeSymbol type) {
		var attrs = type.GetAttributes();
		if (attrs.IsDefaultOrEmpty) {
			return false;
		}

		return attrs.Any(x => x.AttributeClass?.Name is "RecordAsDtoAttribute");
	}

	public static bool IsValueObject(this ITypeSymbol ts) {
		return ts.IsValueType && ts.Interfaces.Any(x => x.Name is "IValueObject" or "IValidatableValueObject");
	}

	public static IFieldSymbol? TryGetValueObjectKey(this ITypeSymbol ts) {
		if (ts.IsValueObject() is false) {
			return null;
		}

		var voKey = ts.GetMembers()
		   .OfType<IFieldSymbol>()
		   .Where(x => x.GetAttributes() is { Length: 1 } attrs && attrs[0].AttributeClass?.Name is "Key")
		   .ToArray();

		return voKey.Length is 1 ? voKey[0] : null;
	}

	public static INamedTypeSymbol? TryGetValueObjectKeyType(this ITypeSymbol ts) {
		var nullable = ts.IsValueNullable();
		INamedTypeSymbol? nt = null;
		if (nullable) {
			nt = (INamedTypeSymbol)ts;
			ts = nt.TypeArguments[0];
			nt = nt.OriginalDefinition;
		}

		if (ts.IsValueObject() is false) {
			return null;
		}

		var voInterface = ts.Interfaces
		   .FirstOrDefault(
				x => x is {
					OriginalDefinition: {
						Name: "IValueObject" or "IValidatableValueObject",
						TypeParameters.Length: 1
					},
					TypeArguments.Length: 1
				}
			);

		if (voInterface is null) {
			return null;
		}

		var voType = voInterface.TypeArguments[0];
		if (nullable) {
			voType = nt!.Construct(voType);
		}

		return voType as INamedTypeSymbol;
	}

	public static T? TryGetArg<T>(this AttributeData data, int index = 0) {
		var args = data.ConstructorArguments;
		if (args.IsDefaultOrEmpty) {
			return default;
		}

		return args[index].As<T>();
	}

	public static INamedTypeSymbol? TryGetType(this ref GeneratorSyntaxContext gsc, string fullyQualifiedMetadataName) =>
		gsc.SemanticModel.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
}
