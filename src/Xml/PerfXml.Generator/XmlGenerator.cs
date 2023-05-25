namespace PerfXml.Generator;

[Generator]
public sealed partial class XmlGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var valueObjects = context.SyntaxProvider
           .CreateSyntaxProvider(SyntaxFilter, SyntaxTransform)
           .Where(x => x is not null)
           .Select((nts, ct) => nts!)
           .Collect();

        context.RegisterSourceOutput(context.ParseOptionsProvider.Combine(valueObjects), CodeGeneration);
    }

    static bool SyntaxFilter(SyntaxNode node, CancellationToken ct) {
        if (node is ClassDeclarationSyntax cls) {
            var attributeCheck = cls.AttributeLists.Any(
                x => x.Attributes
                   .Any(y => y.Name.ToString() is "XmlCls" or "XmlClsAttribute")
            );
            var partialCheck = cls.Modifiers.Any(SyntaxKind.PartialKeyword);
            return attributeCheck && partialCheck;
        }

        if (node is RecordDeclarationSyntax rec) {
            var attributeCheck = rec.AttributeLists.Any(
                x => x.Attributes
                   .Any(y => y.Name.ToString() is "XmlCls" or "XmlClsAttribute")
            );
            var partialCheck = rec.Modifiers.Any(SyntaxKind.PartialKeyword);
            return attributeCheck && partialCheck;
        }

        return false;
    }

    const string XmlSerializationInterfaceName = "IXmlSerialization";
    const string BodyAttributeName = "XmlBody";
    const string FieldAttributeName = "XmlAttribute";
    const string ClassAttributeFullName = "XmlCls";
    const string SplitStringAttributeName = "XmlSplitStr";

    static ClassGenInfo? SyntaxTransform(GeneratorSyntaxContext context, CancellationToken ct) {
        static AttributeSyntax? TryGetClsAttribute(TypeDeclarationSyntax typeDeclarationSyntax) {
            foreach (var attrs in typeDeclarationSyntax.AttributeLists) {
                foreach (var attr in attrs.Attributes) {
                    if (attr.Name.ToString() is "XmlCls" or "XmlClsAttribute") {
                        return attr;
                    }
                }
            }

            return null;
        }

        static ClassGenInfo CreateClassGenInfo(
            AttributeSyntax clsAttribute,
            TypeDeclarationSyntax typeDeclarationSyntax,
            INamedTypeSymbol symbol
        ) {
            var clsGenInfo = new ClassGenInfo(symbol);
            string? xmlNodeName = null;
            if (clsAttribute.ArgumentList is not null) {
                var v = clsAttribute.ArgumentList.Arguments[0].Expression;
                if (v is LiteralExpressionSyntax les) {
                    xmlNodeName = les.Token.ValueText;
                }
            }

            if (xmlNodeName is null) {
                // try get from ancestor
                var ancestorClsName = symbol.GetAllAncestors()
                   .Select(
                        t => {
                            var tryGetAttr = t.TryGetAttribute(ClassAttributeFullName);
                            if (tryGetAttr is not null and { ConstructorArguments.Length: 1 }) {
                                return tryGetAttr.ConstructorArguments[0].Value as string;
                            } else {
                                return null;
                            }
                        }
                    )
                   .Where(x => x is not null)
                   .FirstOrDefault();

                if (ancestorClsName is not null) {
                    xmlNodeName = ancestorClsName;
                    clsGenInfo.InheritedClassName = true;
                } else {
                    xmlNodeName = symbol.Name;
                }
            }
            clsGenInfo.ClassName = xmlNodeName;

            return clsGenInfo;
        }

        var typeSyntax = (TypeDeclarationSyntax)context.Node;
        var clsAttribute = TryGetClsAttribute(typeSyntax);
        if (clsAttribute is null) {
            return null;
        }
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeSyntax, ct)!;
        var classGenInfo = CreateClassGenInfo(clsAttribute, typeSyntax, symbol);

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
            var fieldAttr = field.TryGetAttribute(FieldAttributeName);
            var bodyAttr = field.TryGetAttribute(BodyAttributeName);
            var splitAttr = field.TryGetAttribute(SplitStringAttributeName);

            if (fieldAttr is null && bodyAttr is null) {
                continue;
            }

            var fieldInfo = new FieldGenInfo(field) {
                TypeIsSerializable = field.Type.IsPrimitive() is false
                 && (field.Type.IsList()
                            ? ((INamedTypeSymbol)field.Type).TypeArguments[0]
                            : field.Type
                        ) switch {
                            INamedTypeSymbol nt => nt.TryGetAttribute(BodyAttributeName) is not null,
                            _                   => false
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

        var properties = typeSyntax.Members.OfType<PropertyDeclarationSyntax>()
           .Where(
                x => x.Modifiers.Any(SyntaxKind.StaticKeyword) is false
                 && x.Modifiers.Any(SyntaxKind.IndexerDeclaration) is false
            );

        foreach (var prop in properties) {
            var attributeAttr = prop.AttributeLists.TryGetAttribute(FieldAttributeName);
            var bodyAttr = prop.AttributeLists.TryGetAttribute(BodyAttributeName);
            var splitAttr = prop.AttributeLists.TryGetAttribute(SplitStringAttributeName);

            if (attributeAttr is null && bodyAttr is null) {
                continue;
            }

            var propSymbol = context.SemanticModel.GetDeclaredSymbol(prop, ct);
            if (propSymbol is null) {
                continue;
            }

            var propInfo = new PropertyGenInfo(propSymbol) {
                TypeIsSerializable = TypeIsXmlSerializable(propSymbol.Type, ct)
            };
            static bool TypeIsXmlSerializable(ITypeSymbol type, CancellationToken ct) {
                if (type is not INamedTypeSymbol nt || nt.IsPrimitive()) {
                    return false;
                }

                if (nt.IsList()) {
                    type = nt.TypeArguments[0];
                }

                if (type is not INamedTypeSymbol innerNt || innerNt.IsPrimitive()) {
                    return false;
                }

                var checkInterfaceBySymbol = innerNt.OriginalDefinition.Interfaces.Any(x => x.Name is XmlSerializationInterfaceName);
                var checkInterfaceBySyntax = TryGetOriginalSymbolDefinitionBySyntax(innerNt.DeclaringSyntaxReferences, ct);
                static bool TryGetOriginalSymbolDefinitionBySyntax(ImmutableArray<SyntaxReference> references, CancellationToken ct) {
                    foreach (var sr in references) {
                        var node = sr.GetSyntax(ct);
                        if (node is TypeDeclarationSyntax { BaseList.Types.Count: >= 1 } tds) {
                            foreach (var bt in tds.BaseList.Types) {
                                if (bt.Type is IdentifierNameSyntax ins && ins.Identifier.ValueText is XmlSerializationInterfaceName) {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                }

                if (checkInterfaceBySymbol || checkInterfaceBySyntax) {
                    return true;
                }

                return false;
            }

            if (bodyAttr is not null) {
                var les = bodyAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax;
                if (les?.Token.Value is true && propInfo.Type is INamedTypeSymbol nt) {
                    propInfo.XmlName = GetClsNameForSerializationType(nt);
                } else if (les?.Token.Value is string s) {
                    propInfo.XmlName = s;
                } else {
                    propInfo.XmlName = propSymbol.Name;
                }
                classGenInfo.XmlBodies.Add(propInfo);
            } else if (attributeAttr is not null) {
                propInfo.XmlName = attributeAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax les
                    ? les.Token.Value as string
                    : propSymbol.Name;
                classGenInfo.XmlAttributes.Add(propInfo);
            }

            if (splitAttr is not null) {
                var splitChar = splitAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax les
                    ? les.Token.Value as char?
                    : null;
                propInfo.SplitChar = splitChar;
            }
        }

        return classGenInfo;

        string GetClsNameForSerializationType(INamedTypeSymbol typeSymbol) {
            var nameFromClsAttribute = typeSymbol.TryGetAttribute(ClassAttributeFullName)?.ConstructorArguments[0].As<string>();
            if (nameFromClsAttribute is not null) {
                return nameFromClsAttribute;
            }

            var ancestor = typeSymbol.GetAllAncestors()
               .FirstOrDefault(
                    t =>
                        t.TryGetAttribute(ClassAttributeFullName)?.ConstructorArguments[0].Value is not null
                );
            return ancestor is not null
                ? ancestor.GetAttribute(ClassAttributeFullName).ConstructorArguments[0].Value!.ToString()
                : typeSymbol.Name;
        }
    }

    static void CodeGeneration(SourceProductionContext context, (ParseOptions, ImmutableArray<ClassGenInfo>) tuple) {
        var (parseOptions, types) = tuple;
        if (types.IsDefaultOrEmpty) {
            return;
        }

        var isSharp11 = parseOptions is CSharpParseOptions {
            LanguageVersion: LanguageVersion.CSharp11
        };

        var typesGroupedByNamespace = types
           .ToLookup(x => x.Symbol.ContainingNamespace, SymbolEqualityComparer.Default);

        foreach (var a in typesGroupedByNamespace) {
            var group = a.ToArray();
            var ns = a.Key!.ToString()!;
            var sourceCode = ProcessClasses(context, ns, group, isSharp11);

            context.AddSource($"{nameof(XmlGenerator)}_{ns}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    static string ProcessClasses(
        SourceProductionContext context,
        string containingNamespace,
        ClassGenInfo[] classes,
        bool isSharp11
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
                if (isSharp11) {
                    writer.WriteLine(
                        "static {0}ReadOnlySpan<char> IXmlSerialization.GetNodeName() => \"{1}\";",
                        cls.AdditionalInheritanceMethodModifiers,
                        cls.ClassName
                    );
                } else {
                    writer.WriteLine(
                        "{0}ReadOnlySpan<char> IXmlSerialization.GetNodeName() => \"{1}\";",
                        cls.AdditionalInheritanceMethodModifiers,
                        cls.ClassName
                    );
                }
            }

            WriteParseBody(writer, cls);
            WriteParseAttribute(writer, cls);
            WriteSerializeBody(writer, cls);
            WriteSerializeAttributes(writer, cls);
            WriteSerialize(writer, cls);

            writer.Indent--;
            writer.WriteLine("}");
        }

        if (isSharp11) {
            writer.WriteLine(HiddenInterfaceMethodsSharp11);
        } else {
            writer.WriteLine(HiddenInterfaceMethods);
        }

        var resultStr = writer.InnerWriter.ToString()!;
        return resultStr;
    }

    const string HiddenInterfaceMethods = $$"""
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
""";

    const string HiddenInterfaceMethodsSharp11 = $$"""
file static class __HiddenInterfaceMethods {
    {{AggressiveInlining}}
    public static ReadOnlySpan<char> GetNodeName<T>(this T _) where T : IXmlSerialization => T.GetNodeName();
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
""";
}
