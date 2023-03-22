namespace PerfXml.Generator;

partial class XmlGenerator {
    static void WriteParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
        var needsInlineBody = false;
        var needsSubBody = false;

        foreach (var body in cls.XmlBodies) {
            if (body.OriginalType.IsPrimitive() && body.XmlName is null) {
                needsInlineBody = true;
            } else {
                needsSubBody = true;
            }
        }

        if (needsInlineBody && needsSubBody) {
            throw new($"{cls.Symbol.Name} needs inline body and sub body");
        }

        if (needsInlineBody) {
            writer.WriteLine(
                $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end, IXmlFormatterResolver resolver)"
            );
            using (NestedScope.Start(writer)) {
                foreach (var body in cls.XmlBodies) {
                    if (body.OriginalType.Name == "String") {
                        writer.WriteLines(
                            "var nodeSpan = buffer.ReadNodeValue(innerBodySpan, out endInner);",
                            $"this.{body.Symbol.Name} = resolver.Parse<{body.TypeName}>(nodeSpan);"
                        );
                    } else {
                        throw new(
                            $"Xml:WriteParseBodyMethods: how to inline body {body.OriginalType.IsNativeIntegerType}"
                        );
                    }
                }

                writer.WriteLine("return true;");
            }

            WriteEmptyParseSubBody(writer, cls);
            WriteEmptyParseSubBodyByNames(writer, cls);
        } else {
            if (cls.InheritedFromSerializable is false) {
                WriteEmptyParseBody(writer, cls);
            }

            if (needsSubBody) {
                WriteParseSubBody(writer, cls);
                WriteParseSubBodyByNames(writer, cls);
            } else if (cls.InheritedFromSerializable is false) {
                WriteEmptyParseSubBody(writer, cls);
                WriteEmptyParseSubBodyByNames(writer, cls);
            }
        }
    }

    static void WriteParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
        var xmlBodies = cls.XmlBodies
           .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
           .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
           .Where(x => x.OriginalType is not ITypeParameterSymbol)
           .ToArray();

        if (xmlBodies.Any() is false && cls.InheritedFromSerializable) {
            return;
        }

        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
        );
        if (xmlBodies.Any() is false) {
            writer.WriteLine("  => default;");
            return;
        }

        using (NestedScope.Start(writer)) {
            if (cls.InheritedFromSerializable) {
                writer.WriteLine(
                    "if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner, resolver)) return true;"
                );
            }

            writer.WriteLine("switch (hash) {");
            foreach (var body in xmlBodies) {
                var isList = body.OriginalType.IsList();

                var nameToCheck = body.XmlName
                 ?? throw new InvalidDataException($"no body name for {body.Symbol} in {cls.Symbol}");

                writer.WriteLine($"case {HashName(new(nameToCheck.ToCharArray()))}: {{");
                writer.Indent++;
                if (body.TypeIsSerializable) {
                    if (isList) {
                        var typeToRead = ((INamedTypeSymbol)body.Type).OriginalDefinition.TypeArguments[0];
                        writer.WriteLines(
                            $"this.{body.Symbol.Name} ??= new();",
                            $"this.{body.Symbol.Name}.Add(buffer.Read<{typeToRead}>(bodySpan, out end, resolver));"
                        );
                    } else {
                        writer.WriteLines(
                            $"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body {body.Symbol.Name}\");",
                            $"this.{body.Symbol.Name} = buffer.Read<{body.Type.OriginalDefinition}>(bodySpan, out end, resolver);"
                        );
                    }
                } else {
                    writer.WriteLines(
                        "var nodeSpan = buffer.ReadNodeValue(innerBodySpan, out endInner);",
                        $"this.{body.Symbol.Name} = resolver.Parse<{body.TypeName}>(nodeSpan);"
                    );
                }

                writer.WriteLine("return true; }");
                writer.Indent--;
            }
            writer.WriteLine("}");

            writer.WriteLine("return false;");
        }
    }

    static void WriteParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
        var xmlBodies = cls.XmlBodies
           .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
           .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
           .Where(x => x.OriginalType is ITypeParameterSymbol)
           .ToArray();

        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
        );
        if (xmlBodies.Any() is false) {
            writer.WriteLine("  => default;");
            return;
        }

        using (NestedScope.Start(writer)) {
            if (cls.InheritedFromSerializable) {
                writer.WriteLine(
                    "if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner, resolver)) return true;"
                );
            }

            var isFirst = true;
            foreach (var body in xmlBodies) {
                if (isFirst is false) {
                    writer.Write("else ");
                } else {
                    isFirst = false;
                }

                writer.WriteLine(
                    $"if (nodeName.Equals(NodeNamesCollector.GetFor<{body.Type}>(), StringComparison.Ordinal))"
                );
                using (NestedScope.Start(writer)) {
                    writer.WriteLines(
                        $"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body this.{body.Symbol.Name}\");",
                        $"this.{body.Symbol.Name} = buffer.Read<{body.OriginalType}>(bodySpan, out end, resolver);",
                        "return true;"
                    );
                }
            }

            writer.WriteLine("return false;");
        }
    }

    static void WriteParseAttribute(IndentedTextWriter writer, ClassGenInfo cls) {
        var xmlAttrs = cls.XmlAttributes
           .Where(x => x.XmlName is not null)
           .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
           .OrderBy(x => x.XmlName!.Length)
           .ToArray();

        if (xmlAttrs.Any() is false && cls.InheritedFromSerializable) {
            return;
        }

        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseAttribute(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> value, IXmlFormatterResolver resolver)"
        );
        if (xmlAttrs.Any() is false) {
            writer.WriteLine("  => default;");
            return;
        }

        using (NestedScope.Start(writer)) {
            if (cls.InheritedFromSerializable) {
                writer.WriteLine("if (base.ParseAttribute(ref buffer, hash, value, resolver)) return true;");
            }

            writer.WriteLine("switch (hash) {");
            foreach (var attr in xmlAttrs) {
                writer.WriteLine($"case {HashName(attr.XmlName!.ToCharArray())}: {{");
                writer.Indent++;
                if (attr.SplitChar is not null) {
                    var namedType = (INamedTypeSymbol)attr.OriginalType;
                    var typeToRead = namedType.TypeArguments[0].Name;

                    writer.WriteLine($"var lst = new List<{typeToRead}>();");
                    writer.WriteLine($"var reader = new StrReader(value, '{attr.SplitChar}');");

                    writer.WriteLine("while (reader.HasRemaining())");
                    using (NestedScope.Start(writer)) {
                        writer.WriteLines(
                            $"var val = reader.ReadAndParse<{attr.Type}()>;",
                            "lst.Add(val);"
                        );
                    }

                    writer.WriteLine($"this.{attr.Symbol.Name} = lst;");
                } else {
                    writer.WriteLine($"this.{attr.Symbol.Name} = resolver.Parse<{attr.TypeName}>(value);");
                }

                writer.WriteLine("return true; }");
                writer.Indent--;
            }
            writer.WriteLine("}");

            writer.WriteLine("return false;");
        }
    }

    static void WriteSerializeBody(IndentedTextWriter writer, ClassGenInfo cls) {
        if (cls.XmlBodies.Any() is false && cls.InheritedFromSerializable) {
            return;
        }

        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}void IXmlSerialization.SerializeBody(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver)"
        );
        using (NestedScope.Start(writer)) {
            if (cls.XmlBodies.Any() is false) {
                writer.WriteLine("return;");
                NestedScope.CloseLast();
                return;
            }

            if (cls.InheritedFromSerializable) {
                writer.WriteLine("base.SerializeBody(ref buffer, resolver);");
            }

            foreach (var body in cls.XmlBodies) {
                var isCanBeNull = body.OriginalType.IsReferenceType || body.Type.IsValueNullable();

                NestedScope? isNotNullScope = null;
                if (isCanBeNull) {
                    writer.WriteLine($"if (this.{body.Symbol.Name} is not null)");
                    isNotNullScope = NestedScope.Start(writer);
                }

                if (body.OriginalType.IsList()) {
                    if (body.OriginalType.IsPrimitive() || body.TypeIsSerializable is false) {
                        throw new("for xml body of type list<T>, T must be IXmlSerialization");
                    }

                    writer.WriteLine($"foreach (IXmlSerialization obj in this.{body.Symbol.Name}) {{");
                    writer.WriteLine("    obj.Serialize(ref buffer, resolver); }");
                }
                // another IXmlSerialization
                else if (body.OriginalType.IsPrimitive() is false) {
                    var nodeNameArg = body.XmlName is not null ? $", \"{body.XmlName}\"" : null;
                    writer.WriteLine($"((IXmlSerialization)this.{body.Symbol.Name}).Serialize(ref buffer, resolver{nodeNameArg});");
                } else {
                    writer.WriteLine(
                        body.XmlName is not null
                            ? $"buffer.WriteNodeValue(\"{body.XmlName}\", this.{body.Symbol.Name}, resolver);"
                            : $"buffer.Write(this.{body.Symbol.Name}, resolver);"
                    );
                }

                isNotNullScope?.Close();
            }
        }
    }

    static void WriteSerializeAttributes(IndentedTextWriter writer, ClassGenInfo cls) {
        if (cls.XmlAttributes.Any() is false && cls.InheritedFromSerializable) {
            return;
        }

        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}void IXmlSerialization.SerializeAttributes(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver)"
        );

        using (NestedScope.Start(writer)) {
            if (cls.InheritedFromSerializable) {
                writer.WriteLine("base.SerializeAttributes(ref buffer, resolver);");
            }

            if (cls.XmlAttributes.Any() is false) {
                writer.WriteLine("return;");
                NestedScope.CloseLast();
                return;
            }

            foreach (var field in cls.XmlAttributes) {
                if (field.SplitChar is not null) {
                    using (NestedScope.Start(writer)) {
                        writer.WriteLines(
                            $"using var writer = new StrWriter('{field.SplitChar}');",
                            $"foreach (var val in {field.Symbol.Name})"
                        );
                        using (NestedScope.Start(writer)) {
                            writer.WriteLine("writer.Write(val, resolver);");
                        }

                        writer.WriteLines(
                            $"buffer.WriteAttribute(\"{field.XmlName}\", writer.BuiltSpan, resolver);",
                            "writer.Dispose();"
                        );
                    }
                } else {
                    var writerAction = GetPutAttributeAction(field);
                    writer.WriteLine(writerAction);
                }
            }
        }
    }

    static void WriteSerialize(IndentedTextWriter writer, ClassGenInfo cls) {
        if (cls.InheritedFromSerializable) {
            return;
        }

        writer.WriteLine(
            "void IXmlSerialization.Serialize(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver, ReadOnlySpan<char> nodeName)"
        );
        using (NestedScope.Start(writer)) {
            writer.WriteLines(
                "var node = buffer.StartNodeHead(nodeName.IsEmpty ? this.GetNodeName() : nodeName);",
                "this.SerializeAttributes(ref buffer, resolver);",
                "this.SerializeBody(ref buffer, resolver);",
                "buffer.EndNode(ref node);"
            );
        }
    }

    static string GetPutAttributeAction(BaseMemberGenInfo m) {
        var type = m.Type;
        var name = m.Symbol.Name;
        string? preCheck = null;
        if (m.Type.IfValueNullableGetInnerType() is { } t) {
            type = t;
            preCheck = $"if ({name}.HasValue) ";
            name += ".Value";
        }

        if (type.IsEnum()) {
            return $"{preCheck}buffer.PutEnumValue(\"{m.XmlName}\", {name})";
        }

        var writerAction = type.Name switch {
            "String" => $"buffer.WriteAttribute(\"{m.XmlName}\", {name}, resolver);",
            "Byte"
                or "Int16"
                or "Int32"
                or "UInt32"
                or "Int64"
                or "Double"
                or "Decimal"
                or "Char"
                or "Boolean"
                or "Guid"
                or "DateOnly"
                or "TimeOnly"
                or "DateTime" => $"buffer.WriteAttribute(\"{m.XmlName}\", {name}, resolver);",
            _ => throw new($"no attribute writer for type {type}")
        };
        return $"{preCheck}{writerAction}";
    }

    static ulong HashName(ReadOnlySpan<char> name) {
        var hashedValue = 0x2AAAAAAAAAAAAB67ul;
        for (var i = 0; i < name.Length; i++) {
            hashedValue += name[i];
            hashedValue *= 0x2AAAAAAAAAAAAB6Ful;
        }

        return hashedValue;
    }
}
