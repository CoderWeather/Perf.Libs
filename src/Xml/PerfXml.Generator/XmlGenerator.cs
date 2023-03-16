namespace PerfXml.Generator;

[Generator]
public sealed partial class XmlGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var valueObjects = context.SyntaxProvider
           .CreateSyntaxProvider(SyntaxFilter, SyntaxTransform)
           .Where(x => x is not null)
           .Select((nts, ct) => nts!)
           .Collect();

        var models = context.SyntaxProvider
           .CreateSyntaxProvider(
                static (context, ct) => {
                    //
                    return false;
                },
                static (context, ct) => {
                    return context.Node;
                    //
                }
            );

        context.RegisterSourceOutput(context.CompilationProvider.Combine(valueObjects), CodeGeneration);
    }

    static bool SyntaxFilter(SyntaxNode node, CancellationToken ct) {
        if (node is ClassDeclarationSyntax cls) {
            var attributeCheck = cls.AttributeLists.Any(
                x => x.Attributes
                   .Any(y => y.Name.ToString() is "XmlCls" or "XmlClsAttribute")
            );
            var partialCheck = cls.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));
            return attributeCheck && partialCheck;
        }

        if (node is RecordDeclarationSyntax rec) {
            var attributeCheck = rec.AttributeLists.Any(
                x => x.Attributes
                   .Any(y => y.Name.ToString() is "XmlCls" or "XmlClsAttribute")
            );
            var partialCheck = rec.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));
            return attributeCheck && partialCheck;
        }

        return false;
    }

    static ClassGenInfo? SyntaxTransform(GeneratorSyntaxContext context, CancellationToken ct) {
        var bodyAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("PerfXml.XmlBodyAttribute");
        var fieldAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("PerfXml.XmlFieldAttribute");
        var classAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("PerfXml.XmlClsAttribute");
        var splitStringAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("PerfXml.XmlSplitStrAttribute");

        if (bodyAttributeSymbol is null
         || fieldAttributeSymbol is null
         || classAttributeSymbol is null
         || splitStringAttributeSymbol is null) {
            return null;
        }

        ClassGenInfo? ParseTypeToClass(INamedTypeSymbol namedType) {
            var clsAttribute = namedType.TryGetAttribute(classAttributeSymbol);
            if (clsAttribute is null) {
                return null;
            }

            var clsGen = new ClassGenInfo(namedType) {
                ClassName = clsAttribute.ConstructorArguments[0].As<string>(),
                InheritedFromSerializable = namedType.BaseType?.ToString() is not (null or "Object")
                 && namedType.BaseType.TryGetAttribute(classAttributeSymbol) is not null
            };

            if (clsGen.ClassName is null) {
                var ancestor = namedType.GetAllAncestors()
                   .FirstOrDefault(
                        t =>
                            t.TryGetAttribute(classAttributeSymbol)?.ConstructorArguments[0].Value is not null
                    );
                if (ancestor is not null) {
                    clsGen.ClassName =
                        ancestor.GetAttribute(classAttributeSymbol).ConstructorArguments[0].Value!.ToString();
                    clsGen.InheritedClassName = true;
                } else {
                    clsGen.ClassName = namedType.Name;
                }
            }

            return clsGen;
        }

        var symbol = context.Node switch {
            ClassDeclarationSyntax cls  => context.SemanticModel.GetDeclaredSymbol(cls, ct),
            RecordDeclarationSyntax rec => context.SemanticModel.GetDeclaredSymbol(rec, ct),
            _                           => null
        };
        if (symbol is null) {
            return null;
        }

        var classGenInfo = ParseTypeToClass(symbol);
        if (classGenInfo is null) {
            return null;
        }

        var fields = symbol.GetMembers()
           .OfType<IFieldSymbol>()
           .Where(
                f => f is {
                    AssociatedSymbol: null,
                    IsConst : false,
                    IsStatic : false
                }
            );
        foreach (var field in fields) {
            var fieldAttr = field.TryGetAttribute(fieldAttributeSymbol);
            var bodyAttr = field.TryGetAttribute(bodyAttributeSymbol);
            var splitAttr = field.TryGetAttribute(splitStringAttributeSymbol);

            if (fieldAttr is null && bodyAttr is null) {
                continue;
            }

            var fieldInfo = new FieldGenInfo(field) {
                TypeIsSerializable = field.Type.IsPrimitive() is false
                 && (field.Type.IsList()
                            ? ((INamedTypeSymbol)field.Type).OriginalDefinition.TypeArguments[0]
                            : field.Type
                        ) switch {
                            INamedTypeSymbol nt => (nt.IsDefinition ? nt : nt.OriginalDefinition)
                               .TryGetAttribute(classAttributeSymbol) is not null,
                            _ => false
                        }
            };
            if (fieldAttr is not null) {
                fieldInfo.XmlName = fieldAttr.ConstructorArguments[0].As<string>() ?? field.Name;
                classGenInfo.XmlAttributes.Add(fieldInfo);
            }

            if (bodyAttr is not null) {
                fieldInfo.XmlName = bodyAttr.ConstructorArguments[0].As<string>() ?? field.Name;
                classGenInfo.XmlBodies.Add(fieldInfo);
            }

            if (splitAttr?.ConstructorArguments[0].As<char>() is { } ch) {
                fieldInfo.SplitChar = ch;
            }
        }

        var properties = symbol.GetMembers()
           .OfType<IPropertySymbol>()
           .Where(
                p => p is {
                    IsStatic : false,
                    IsIndexer: false
                }
            );
        foreach (var prop in properties) {
            var fieldAttr = prop.TryGetAttribute(fieldAttributeSymbol);
            var bodyAttr = prop.TryGetAttribute(bodyAttributeSymbol);
            var splitAttr = prop.TryGetAttribute(splitStringAttributeSymbol);

            if (fieldAttr is null && bodyAttr is null) {
                continue;
            }

            var propInfo = new PropertyGenInfo(prop) {
                TypeIsSerializable = prop.Type.IsPrimitive() is false
                 && (prop.Type.IsList()
                            ? ((INamedTypeSymbol)prop.Type).OriginalDefinition.TypeArguments[0]
                            : prop.Type
                        ) switch {
                            INamedTypeSymbol nt => (nt.IsDefinition ? nt : nt.OriginalDefinition)
                               .TryGetAttribute(classAttributeSymbol) is not null,
                            _ => false
                        }
            };

            if (fieldAttr is not null) {
                propInfo.XmlName = fieldAttr.ConstructorArguments[0].As<string>() ?? prop.Name;
                classGenInfo.XmlAttributes.Add(propInfo);
            }

            if (bodyAttr is not null) {
                var takeNameFromType = bodyAttr.ConstructorArguments[0].Value is true;
                if (takeNameFromType) {
                    propInfo.XmlName = prop.Type switch {
                        INamedTypeSymbol nt => GetClsNameForSerializationType(nt),
                        ITypeParameterSymbol => throw new(
                            "Cannot set name of sub-body entry with name of base of generic type parameter type"
                        ),
                        _ => null
                    };
                } else {
                    propInfo.XmlName = bodyAttr.ConstructorArguments[0].As<string>();
                }

                classGenInfo.XmlBodies.Add(propInfo);
            }

            if (splitAttr?.ConstructorArguments[0].As<char>() is { } ch) {
                propInfo.SplitChar = ch;
            }
        }

        return classGenInfo;

        string GetClsNameForSerializationType(INamedTypeSymbol typeSymbol) {
            var nameFromClsAttribute = typeSymbol.TryGetAttribute(classAttributeSymbol)?.ConstructorArguments[0].As<string>();
            if (nameFromClsAttribute is not null) {
                return nameFromClsAttribute;
            }

            var ancestor = typeSymbol.GetAllAncestors()
               .FirstOrDefault(
                    t =>
                        t.TryGetAttribute(classAttributeSymbol)?.ConstructorArguments[0].Value is not null
                );
            return ancestor is not null
                ? ancestor.GetAttribute(classAttributeSymbol).ConstructorArguments[0].Value!.ToString()
                : typeSymbol.Name;
        }
    }

    static void CodeGeneration(SourceProductionContext context, (Compilation, ImmutableArray<ClassGenInfo>) tuple) {
        var (_, types) = tuple;
        if (types.IsDefaultOrEmpty) {
            return;
        }
        // compilation.Options.

        var typesGroupedByNamespace = types
           .ToLookup(x => x.Symbol.ContainingNamespace, SymbolEqualityComparer.Default);

        foreach (var a in typesGroupedByNamespace) {
            var group = a.ToArray();
            var ns = a.Key!.ToString()!;
            var sourceCode = ProcessClasses(context, ns, group);

            context.AddSource($"{nameof(XmlGenerator)}_{ns}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    static string ProcessClasses(
        SourceProductionContext context,
        string containingNamespace,
        ClassGenInfo[] classes
    ) {
        using var writer = new IndentedTextWriter(new StringWriter(), "	");
        writer.WriteLines(
            $"namespace {containingNamespace};",
            "",
            "using System;",
            "using System.IO;",
            "using System.Collections.Generic;",
            "using PerfXml;",
            // "using PerfXml.Str;",
            ""
        );

        foreach (var cls in classes) {
            writer.WriteLine($"partial {cls.Symbol.DeclarationString()} {cls.Symbol.MinimalName()} {{");
            writer.Indent++;

            if (cls.InheritedClassName is false) {
                writer.WriteLine(
                    "{0}ReadOnlySpan<char> IXmlSerialization.GetNodeName() => \"{1}\";",
                    cls.AdditionalInheritanceMethodModifiers,
                    cls.ClassName
                );
            }

            WriteParseBody(writer, cls);
            WriteParseAttribute(writer, cls);
            WriteSerializeBody(writer, cls);
            WriteSerializeAttributes(writer, cls);
            WriteSerialize(writer, cls);

            writer.Indent--;
            writer.WriteLine("}");
        }

        writer.WriteLine(
            $$"""
file static class __HiddenInterfaceMethods {
    {{AggressiveInlining}}
    public static ReadOnlySpan<char> GetNodeName<T>(this T t) where T : IXmlSerialization => t.GetNodeName();
    {{AggressiveInlining}}
    public static bool ParseFullBody<T>(this T t, ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.ParseFullBody(ref buffer, bodySpan, ref end, resolver);
    {{AggressiveInlining}}
    public static bool ParseSubBody<T>(this T t, ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner, resolver);
    {{AggressiveInlining}}
    public static bool ParseSubBody<T>(this T t, ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.ParseSubBody(ref buffer, nodeName, bodySpan, innerBodySpan, ref end, ref endInner, resolver);
    {{AggressiveInlining}}
    public static bool ParseAttribute<T>(this T t, ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> value, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.ParseAttribute(ref buffer, hash, value, resolver);
    {{AggressiveInlining}}
    public static void SerializeBody<T>(this T t, ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.SerializeBody(ref buffer, resolver);
    {{AggressiveInlining}}
    public static void SerializeAttributes<T>(this T t, ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.SerializeAttributes(ref buffer, resolver);
    {{AggressiveInlining}}
    public static void Serialize<T>(this T t, ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver) where T : IXmlSerialization => t.Serialize(ref buffer, resolver);
}
"""
        );

        var resultStr = writer.InnerWriter.ToString()!;
        return resultStr;
    }
}
