namespace Perf.Holders.Generator.Builders;

using System.Text;
using Internal;
using Types;

sealed class MultiResultSourceBuilder(MultiResultHolderContextInfo context) {
    /*
     * Fun things:
     *   no baseType - like cast to base object, at this moment I don't want to write a 2-8 multiresult copy-paste types
     */
    const string MrMarker = "global::Perf.Holders.IMultiResultHolder";

    const string Exceptions = "global::Perf.Holders.Exceptions.MultiResultHolderExceptions";
    const string DebuggerBrowsableNever = "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]";
    const string EditorBrowsable = "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";
    const string EqualityComparer = "global::System.Collections.Generic.EqualityComparer";

    readonly string enumState = $"{context.MultiResult.OnlyName}State";

    readonly InterpolatedStringBuilder sb = new(stringBuilder: new(8000));

    readonly CompInfo compInfo = context.CompInfo;

    // minimum at 1 because of generated type braces
    int bracesToCloseOnEnd = 1;

    void Preparation() {
        if (compInfo.SupportNullableAnnotation() is false) {
            var els = context.Elements;
            for (var i = 0; i < els.Count; i++) {
                var el = els[i];
                if (el.IsStruct is false) {
                    els[i] = el with {
                        TypeNullable = el.Type
                    };
                }
            }

            // no need to update Elements within context, EquatableList holds elements in common List
        }
    }

    public string WriteAllAndBuild() {
        WriteAll();
        var result = sb.ToString();
        sb.Clear();
        return result;
    }

    void WriteAll() {
        Preparation();
        DeclareTopLevelStatements();
        WriteDeclarationClasses();
        WriteDebugView();
        if (context.Configuration.OpenState is true) {
            WriteCustomState();
        }

        WriteTypeAttributes();
        WriteTypeDeclaration();
        if (context.Configuration.OpenState is false) {
            WriteCustomState();
        }

        WriteConstructors();
        WriteFields();
        WriteExplicitInterfaceMembers();
        WriteProperties();
        WriteCastOperators();
        WriteEqualityOperators();
        WriteCastingMethods();
        WriteEqualityMethods();
        WriteGetHashCode();
        WriteToString();
        WriteDebugPrint();
        WriteMapMethods();
        WriteEndOfType();

        WriteEndOfFile();
    }

    void DeclareTopLevelStatements() {
        sb.AppendLine("// <auto-generated />");
        if (compInfo.SupportNullableAnnotation()) {
            sb.AppendLine("#nullable enable");
        }

        if (context.Namespace is not null) {
            if (compInfo.SupportFileScopedNamespace()) {
                sb.AppendLine($"namespace {context.Namespace};");
            } else {
                sb.AppendLine($"namespace {context.Namespace}\n{{");
                bracesToCloseOnEnd++;
            }
        }
    }

    void WriteDebugView() {
        if (compInfo.SupportFileVisibilityModifier() && context.ContainingTypes == default) {
            sb.Append("file ");
        } else {
            sb.AppendLine(EditorBrowsable);
        }

        var typeParameters = context.TypeParameters();
        var typeParametersConstraints = context.TypeParametersConstraints();

        sb.AppendInterpolated($"sealed class DebugView_{context.MultiResult.OnlyName}");

        if (context.MultiResult.TypeParameterCount == 0) {
            sb.AppendLine(" {");
        } else {
            sb.AppendLine(typeParameters);
            sb.Indent++;
            sb.Append(typeParametersConstraints);
            sb.Indent--;
            sb.AppendLine("{");
        }

        sb.Indent++;
        sb.AppendInterpolatedLine(
            $$"""
            public DebugView_{{context.MultiResult.OnlyName}}({{context.MultiResult.DeclarationName}} mr) {
                var stateField = typeof({{context.MultiResult.DeclarationName}})
                    .GetField("state", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!;
                this.State = stateField.GetValue(mr)!;
                this.Value = global::System.Enum.Format(stateField.FieldType, this.State, "G") switch {
            """
        );
        sb.Indent += 2;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"\"{el.Property}\" => mr.{el.Property},");
        }

        sb.Indent -= 2;
        sb.AppendInterpolatedLine(
            $$"""
                    _ => "Default"
                };
            }
            public object State { get; }
            public object Value { get; }
            """
        );
        sb.Indent--;
        sb.AppendLine("}");
    }
    // ReSharper restore ConvertIfStatementToConditionalTernaryExpression

    void WriteCustomState() {
        var accessibilityModifier = context.Configuration.OpenState is true
            && context.MultiResult.Accessibility is TypeAccessibility.Public
                ? "public "
                : "";
        sb.AppendInterpolatedLine($"{accessibilityModifier}enum {context.MultiResult.OnlyName}State : byte {{");
        sb.Indent++;
        sb.AppendLine("Default = 0,");
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"{el.Property} = {el.Index + 1},");
        }

        sb.Indent--;
        sb.AppendLine("}");
    }

    void WriteDeclarationClasses() {
        if (context.ContainingTypes == default) {
            return;
        }

        for (var i = context.ContainingTypes.Count - 1; i >= 0; i--) {
            var t = context.ContainingTypes[i];
            sb.AppendInterpolatedLine($"partial {t.Kind} {t.Name} {{");
            bracesToCloseOnEnd++;
        }
    }

    void WriteTypeAttributes() {
        var openTypeParameters = context.OpenTypeParameters();

        sb.AppendInterpolatedLine(
            $"""
            [global::System.Diagnostics.DebuggerTypeProxy(typeof(DebugView_{context.MultiResult.OnlyName}{openTypeParameters}))]
            """
        );

        sb.AppendLine(
            """
            [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
            """
        );

        if (context.ShouldGenerateJsonConverter()) {
            sb.AppendInterpolatedLine(
                $"[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof({context.GeneratedJsonConverterTypeForAttribute}))]"
            );
        } else if (compInfo.GenericSerializerSystemTextJsonAvailable) {
            sb.AppendInterpolatedLine(
                $"[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::Perf.Holders.Serialization.SystemTextJson.MultiResultHolderJsonConverterFactory))]"
            );
        }

        if (context.ShouldGenerateMessagePackFormatter()) {
            sb.AppendInterpolatedLine(
                $"[global::MessagePack.MessagePackFormatterAttribute(typeof({context.GeneratedMessagePackFormatterTypeForAttribute()}))]"
            );
        }

        sb.AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]");
    }

    void WriteTypeDeclaration() {
        sb.AppendInterpolatedLine($"readonly partial struct {context.MultiResult.DeclarationName} :");
        sb.Indent++;
        sb.AppendInterpolatedLine($"global::System.IEquatable<{context.MultiResult.DeclarationName}>,");
        foreach (var el in context.Elements) {
            if (el.IsTypeParameter is false) {
                sb.AppendInterpolatedLine($"global::System.IEquatable<{el.Type}>,");
            }
        }

        sb.Length -= 2;
        sb.AppendLine();

        if (context.MultiResult.TypeParameterCount > 0) {
            sb.AppendLine(context.TypeParametersConstraints());
        }

        sb.Indent--;
        sb.AppendLine("{");
        sb.Indent++;
    }

    void WriteConstructors() {
        var mr = context.MultiResult.OnlyName;

        sb.AppendInterpolatedLine($"public {mr}() {{");
        sb.Indent++;
        sb.AppendInterpolatedLine($"this.state = {enumState}.Default;");
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"this.{el.Field} = default!;");
        }

        sb.Indent--;
        sb.AppendLine("}");

        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"public {mr}({el.Type} {el.Field}) {{");
            sb.Indent++;
            sb.AppendInterpolatedLine($"this.state = {enumState}.{el.Property};");
            sb.AppendInterpolatedLine($"this.{el.Field} = {el.Field};");
            foreach (var innerEl in context.Elements) {
                if (innerEl == el) {
                    continue;
                }

                sb.AppendInterpolatedLine($"this.{innerEl.Field} = default!;");
            }

            sb.Indent--;
            sb.AppendLine("}");
        }
    }

    void WriteFields() {
        sb.AppendInterpolatedLine($"readonly {enumState} state;");

        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"readonly {el.Type} {el.Field};");
        }
    }

    void WriteExplicitInterfaceMembers() {
        if (context.Elements.Any(x => x.HavePartial) is false) {
            return;
        }

        var typedMarkerSb = new StringBuilder();
        typedMarkerSb.Append(MrMarker);
        typedMarkerSb.Append('<');
        foreach (var el in context.Elements) {
            typedMarkerSb.AppendInterpolated($"{el.Type},");
        }

        typedMarkerSb.Length--;
        typedMarkerSb.Append('>');

        var typedMarker = typedMarkerSb.ToString();

        foreach (var el in context.Elements) {
            if (el.HavePartial is false) {
                continue;
            }

            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                {{el.Type}} {{typedMarker}}.{{MultiResultHolderContextInfo.MultiResultElementInfo.Properties[el.Index]}} => {{el.Property}};
                """
            );
            /*sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                {{el.Type}}{{typedMarker}}.Is{{MultiResultHolderContextInfo.MultiResultElementInfo.Properties[el.Index]}} => Is{{el.Property}};
                """
            );*/
        }
    }

    void WriteProperties() {
        sb.AppendInterpolatedLine(
            $$"""
            {{DebuggerBrowsableNever}}
            public {{enumState}} State => this.state;
            """
        );

        foreach (var el in context.Elements) {
            if (context.Configuration.AddIsProperties is true) {
                if (el.StateCheck.HavePartial) {
                    sb.AppendInterpolatedLine(
                        $$"""
                        {{DebuggerBrowsableNever}}
                        public partial bool {{el.StateCheck.Property}} => state == {{enumState}}.{{el.Property}};
                        """
                    );
                } else {
                    sb.AppendInterpolatedLine(
                        $$"""
                        {{DebuggerBrowsableNever}}
                        public bool {{el.StateCheck.Property}} => state == {{enumState}}.{{el.Property}};
                        """
                    );
                }
            }

            if (el.HavePartial) {
                sb.AppendInterpolatedLine(
                    $$"""
                    {{DebuggerBrowsableNever}}
                    public partial {{el.Type}} {{el.Property}} =>
                    """
                );
            } else {
                sb.AppendInterpolatedLine(
                    $$"""
                    {{DebuggerBrowsableNever}}
                    public {{el.Type}} {{el.Property}} =>
                    """
                );
            }

            sb.Indent++;
            sb.AppendLine("state switch {");
            sb.Indent++;
            sb.AppendInterpolatedLine($"{enumState}.{el.Property} => {el.Field},");
            foreach (var innerEl in context.Elements) {
                if (el == innerEl) {
                    continue;
                }

                sb.AppendInterpolatedLine(
                    $"{enumState}.{innerEl.Property} => throw {Exceptions}.WrongAccess<{context.MultiResult.DeclarationName}>(nameof({enumState}.{innerEl.Property}), nameof({enumState}.{el.Property})),"
                );
            }

            sb.AppendLine($"_ => throw {Exceptions}.Default<{context.MultiResult.DeclarationName}>()");
            sb.Indent--;
            sb.AppendLine("};");
            sb.Indent--;
        }
    }

    void WriteCastOperators() {
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine(
                $$"""
                public static implicit operator {{context.MultiResult.DeclarationName}}({{el.Type}} {{el.Field}}) => new({{el.Field}}: {{el.Field}});
                """
            );
        }
    }

    void WriteEqualityOperators() {
        var mr = context.MultiResult.DeclarationName;
        sb.AppendInterpolatedLine(
            $$"""
            public static bool operator ==({{mr}} left, {{mr}} right) => left.Equals(right);
            public static bool operator !=({{mr}} left, {{mr}} right) => left.Equals(right) == false;
            """
        );

        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine(
                $$"""
                public static bool operator ==({{mr}} left, {{el.Type}} right) => left.Equals(right);
                public static bool operator !=({{mr}} left, {{el.Type}} right) => left.Equals(right) == false;
                """
            );
        }
    }

    void WriteCastingMethods() {
        // Can be added if there would be base types implemented for all element count
        _ = context;
    }

    void WriteEqualityMethods() {
        sb.AppendLine("public override bool Equals(object? obj) =>");
        sb.Indent++;
        sb.AppendLine("obj switch {");
        sb.Indent++;
        sb.AppendLine("null => false,");
        sb.AppendInterpolatedLine($"{context.MultiResult.DeclarationName} other => Equals(other),");
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"{el.Type} other => Equals(other),");
        }

        sb.AppendLine("_ => false");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;

        sb.AppendInterpolatedLine($"public bool Equals({context.MultiResult.DeclarationName} other) =>");
        sb.Indent++;
        sb.AppendLine("(state, other.state) switch {");
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"({enumState}.{el.Property}, {enumState}.{el.Property}) => {el.Field}.Equals(other.{el.Field}),");
        }

        sb.AppendInterpolatedLine($"({enumState}.Default, {enumState}.Default) => true,");
        sb.AppendLine("_ => false");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;

        foreach (var el in context.Elements) {
            if (el.IsStruct) {
                sb.AppendInterpolatedLine(
                    $"""
                    public bool Equals({el.Type} other) => state == {enumState}.{el.Property} && {el.Field}.Equals(other);
                    """
                );
            } else {
                var type = compInfo.SupportNullableAnnotation() ? el.TypeNullable : el.Type;
                sb.AppendInterpolatedLine(
                    $"""
                    public bool Equals({type} other) => state == {enumState}.{el.Property} && {EqualityComparer}<{type}>.Default.Equals({el.Field}, other);
                    """
                );
            }
        }
    }

    void WriteGetHashCode() {
        sb.AppendLine("public override int GetHashCode() =>");
        sb.Indent++;
        sb.AppendLine("state switch {");
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"({enumState}.{el.Property}) => {el.Field}.GetHashCode(),");
        }

        sb.AppendLine("_ => 0");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;
    }

    void WriteToString() {
        var resultString = compInfo.SupportNullableAnnotation() ? "string?" : "string";
        sb.AppendInterpolatedLine($"public override {resultString} ToString() =>");
        sb.Indent++;
        sb.AppendLine("state switch {");
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"{enumState}.{el.Property} => {el.Field}.ToString(),");
        }

        sb.AppendLine("_ => \"\"");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;
    }

    void WriteDebugPrint() {
        sb.AppendLine("string DebugPrint() =>");
        sb.Indent++;
        sb.AppendLine("state switch {");
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine(
                $$"""
                {{enumState}}.{{el.Property}} => $"{{el.Property}}={{{el.Field}}}",
                """
            );
        }

        sb.AppendLine("_ => \"Default\"");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;
    }

    void WriteMapMethods() {
        // insane thing with map methods on each type
        _ = context;
    }

    void WriteEndOfType() {
        sb.Indent--;
        bracesToCloseOnEnd--;
        sb.AppendLine("}");
    }

    void WriteEndOfFile() {
        for (var i = 0; i < bracesToCloseOnEnd; i++) {
            sb.Append('}');
        }

        sb.AppendLine();
    }
}
