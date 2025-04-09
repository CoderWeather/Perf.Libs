namespace Perf.Holders.Generator.Builders;

using Internal;
using Types;

sealed class OptionSourceBuilder(OptionHolderContextInfo contextInfo) {
    const string BaseOption = "global::Perf.Holders.Option";
    const string OptionMarker = "global::Perf.Holders.IOptionHolder";
    const string Exceptions = "global::Perf.Holders.Exceptions.OptionHolderExceptions";
    const string DebuggerBrowsableNever = "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]";
    const string EditorBrowsable = "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";
    const string EqualityComparer = "global::System.Collections.Generic.EqualityComparer";

    readonly string baseType = $"{BaseOption}<{contextInfo.Some.Type}>";

    readonly string throwDefault = $"throw {Exceptions}.Default<{contextInfo.Option.DeclarationName}, {contextInfo.Some.Type}>()";

    readonly string enumState = $"{contextInfo.Option.OnlyName}State";
    readonly string enumSome = $"{contextInfo.Option.OnlyName}State.{contextInfo.Some.Property}";
    readonly string enumNone = $"{contextInfo.Option.OnlyName}State.{contextInfo.IsSome.Property}";

    readonly InterpolatedStringBuilder sb = new(stringBuilder: new(8000));
    OptionHolderContextInfo context = contextInfo;

    readonly CompInfo compInfo = contextInfo.CompInfo;

    // minimum at 1 because of generated type braces
    int bracesToCloseOnEnd = 1;

    void Preparation() {
        if (compInfo.SupportNullableAnnotation() is false && context.Some.IsStruct is false) {
            context = context with {
                Some = context.Some with {
                    TypeNullable = context.Some.Type
                }
            };
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
        if (context.Configuration.PublicState is true) {
            WriteCustomState();
        }

        WriteTypeAttributes();
        WriteTypeDeclaration();
        if (context.Configuration.PublicState is false) {
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

        var typeArgs = context.Some.IsTypeParameter ? $"<{context.Some.Type}>" : "";
        var typeArgsConstraints = context.Some.IsTypeParameter ? $"where {context.Some.Type} : notnull " : "";
        sb.AppendInterpolatedLine(
            $$"""
            sealed class {{context.Option.OnlyName}}_DebugView{{typeArgs}} {{typeArgsConstraints}}{
                public {{context.Option.OnlyName}}_DebugView({{context.Option.DeclarationName}} opt) {
                    var stateField = typeof({{context.Option.DeclarationName}})
                        .GetField("state", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!;
                    this.State = stateField.GetValue(opt)!;
                    this.Value = global::System.Enum.Format(stateField.FieldType, this.State, "G") switch {
                        "{{context.Some.Property}}" => opt.{{context.Some.Property}},
                        "{{context.IsSome.Property}}" => "{{context.IsSome.Property}}",
                        _ => "Default"
                    };
                }
                public object State { get; }
                public object Value { get; }
            }
            """
        );
    }

    void WriteCustomState() {
        var accessibilityModifier = context.Configuration.PublicState is true
            && context.Option.Accessibility is TypeAccessibility.Public
                ? "public "
                : "";

        sb.AppendInterpolatedLine(
            $$"""
            {{accessibilityModifier}}enum {{context.Option.OnlyName}}State : byte {
                {{context.IsSome.Property}} = 0,
                {{context.Some.Property}} = 1
            }
            """
        );
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
        var typeArg = context.Some.IsTypeParameter ? "<>" : "";
        sb.AppendInterpolatedLine(
            $"""
            [global::System.Diagnostics.DebuggerTypeProxy(typeof({context.Option.OnlyName}_DebugView{typeArg}))]
            """
        );

        sb.AppendLine(
            """
            [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
            """
        );

        if (context.ShouldGenerateJsonConverters()) {
            sb.AppendInterpolatedLine(
                $"[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof({context.GeneratedJsonConverterTypeForAttribute}))]"
            );
        } else if (compInfo.GenericSerializerSystemTextJsonAvailable) {
            sb.AppendInterpolatedLine(
                $"[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::Perf.Holders.Serialization.SystemTextJson.OptionHolderJsonConverterFactory))]"
            );
        }

        if (context.ShouldGenerateMessagePackFormatters()) {
            sb.AppendInterpolatedLine(
                $"[global::MessagePack.MessagePackFormatterAttribute(typeof({context.GeneratedMessagePackFormatterTypeForAttribute()}))]"
            );
        } else if (compInfo.GenericSerializerMessagePackAvailable) {
            sb.AppendInterpolatedLine(
                $"[global::MessagePack.MessagePackFormatterAttribute(typeof(global::Perf.Holders.Serialization.MessagePack.OptionHolderFormatterResolver))]"
            );
        }

        sb.AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]");
    }

    void WriteTypeDeclaration() {
        sb.AppendInterpolated($"readonly partial struct {context.Option.DeclarationName} :");
        sb.Indent++;
        sb.AppendInterpolatedLine($"global::System.IEquatable<{context.Option.DeclarationName}>,");
        sb.AppendInterpolatedLine($"global::System.IEquatable<{baseType}>,");

        sb.AppendInterpolated($"global::System.IEquatable<{context.Some.TypeNullable}>");
        if (context.Some.IsStruct) {
            sb.AppendLine(",");
            sb.AppendInterpolated($"global::System.IEquatable<{context.Some.Type}>");
        }

        if (context.Configuration.IncludeOptionSomeObject is true) {
            sb.AppendLine(",");
            sb.AppendInterpolated($"global::System.IEquatable<{BaseOption}.Some<{context.Some.TypeNullable}>>");
            if (context.Some.IsStruct) {
                sb.AppendLine(",");
                sb.AppendInterpolated($"global::System.IEquatable<{BaseOption}.Some<{context.Some.Type}>>");
            }
        }

        sb.AppendLine();

        if (context.Some.IsTypeParameter) {
            sb.AppendInterpolatedLine($"where {context.Some.Type} : notnull");
        }

        sb.Indent--;
        sb.AppendLine("{");
        sb.Indent++;
    }

    void WriteConstructors() {
        var option = context.Option.OnlyName;
        var someType = context.Some.Type;
        var someTypeNullable = context.Some.TypeNullable;
        var field = context.Some.Field;

        sb.AppendInterpolatedLine(
            $$"""
            public {{option}}() {
                this.state = {{enumSome}};
                this.{{field}} = default!;
            }
            """
        );
        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{someType}} {{field}}) {
                    this.state = {{enumSome}};
                    this.{{field}} = {{field}};
                }
                """
            );
            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{someTypeNullable}} {{field}}) {
                    if ({{field}} != null) {
                        this.state = {{enumSome}};
                        this.{{field}} = {{field}}!.Value;
                    } else {
                        this.state = {{enumNone}};
                        this.{{field}} = default!;
                    }
                }
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{someTypeNullable}} {{field}}) {
                    if ({{field}} != null) {
                        this.state = {{enumSome}};
                        this.{{field}} = {{field}}!;
                    } else {
                        this.state = {{enumNone}};
                        this.{{field}} = default!;
                    }
                }
                """
            );
        }

        if (context.Configuration.IncludeOptionSomeObject is true) {
            if (context.Some.IsStruct) {
                sb.AppendInterpolatedLine(
                    $$"""
                    public {{option}}({{BaseOption}}.Some<{{someType}}> someObject) : this({{field}}: someObject.Value) { }
                    """
                );
            }

            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{BaseOption}}.Some<{{someTypeNullable}}> someObject) : this({{field}}: someObject.Value) { }
                """
            );
        }
    }

    void WriteFields() {
        sb.AppendInterpolatedLine(
            $$"""
            readonly {{enumState}} state;
            readonly {{context.Some.Type}} {{context.Some.Field}};
            """
        );
    }

    void WriteExplicitInterfaceMembers() {
        if (context.Some.Property != OptionHolderContextInfo.SomeInfo.DefaultProperty) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                {{context.Some.Type}} {{OptionMarker}}<{{context.Some.Type}}>.Some => {{context.Some.Property}};
                """
            );
        }

        if (context.IsSome.Property != OptionHolderContextInfo.IsSomeInfo.DefaultProperty) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                bool {{OptionMarker}}<{{context.Some.Type}}>.IsSome => {{context.IsSome.Property}};
                """
            );
        }
    }

    void WriteProperties() {
        if (context.Configuration.PublicState is true) {
            sb.AppendInterpolatedLine(
                $$"""
                public {{enumState}} State => this.state;
                """
            );
        }

        if (context.Some.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial {{context.Some.Type}} {{context.Some.Property}} =>
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public {{context.Some.Type}} {{context.Some.Property}} =>
                """
            );
        }

        sb.Indent++;
        sb.AppendInterpolatedLine(
            $$"""
            state switch {
                {{enumSome}} => {{context.Some.Field}},
                {{enumNone}} => throw {{Exceptions}}.SomeAccessWhenNone<{{context.Option.DeclarationName}}, {{context.Some.Type}}>("{{context.Some.Property}}"),
                _ => {{throwDefault}}
            };
            """
        );
        sb.Indent--;

        if (context.IsSome.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial bool {{context.IsSome.Property}} => state == {{enumSome}};
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public bool {{context.IsSome.Property}} => state == {{enumSome}};
                """
            );
        }
    }

    void WriteCastOperators() {
        var field = context.Some.Field;
        var someType = context.Some.Type;
        var someTypeNullable = context.Some.TypeNullable;
        var option = context.Option.DeclarationName;

        if (context.Configuration.ImplicitCastSomeTypeToOption is true) {
            if (context.Some.IsStruct) {
                sb.AppendInterpolatedLine(
                    $$"""
                    public static implicit operator {{option}}({{someType}} {{field}}) => new({{field}}: {{field}});
                    """
                );
            }

            sb.AppendInterpolatedLine(
                $$"""
                public static implicit operator {{option}}({{someTypeNullable}} {{field}}) => new({{field}}: {{field}});
                """
            );
        }

        if (context.Configuration.IncludeOptionSomeObject is true) {
            if (context.Some.IsStruct) {
                sb.AppendInterpolatedLine(
                    $"public static implicit operator {option}({BaseOption}.Some<{someType}> someObject) => new(someObject: someObject);"
                );
            }

            sb.AppendInterpolatedLine(
                $"public static implicit operator {option}({BaseOption}.Some<{someTypeNullable}> someObject) => new(someObject: someObject);"
            );
        }

        sb.AppendInterpolatedLine(
            $$"""
            public static implicit operator {{option}}({{BaseOption}}.None _) => default;
            public static implicit operator {{option}}(global::Perf.Holders.Unit _) => default;
            public static implicit operator {{option}}({{BaseOption}}<{{someType}}> o) => o.IsSome ? new({{field}}: o.Some) : default;
            public static implicit operator {{BaseOption}}<{{someType}}>({{option}} o) => o.{{context.IsSome.Property}} ? new(some: o.{{field}}) : default;
            public static implicit operator bool({{option}} o) => o.{{context.IsSome.Property}};
            """
        );
    }

    void WriteEqualityOperators() {
        var option = context.Option.DeclarationName;
        var someType = context.Some.Type;
        var someTypeNullable = context.Some.TypeNullable;

        sb.AppendInterpolatedLine(
            $$"""
            public static bool operator ==({{option}} left, {{option}} right) => left.Equals(right);
            public static bool operator !=({{option}} left, {{option}} right) => left.Equals(right) == false;
            public static bool operator ==({{option}} left, {{baseType}} right) => left.Equals(right);
            public static bool operator !=({{option}} left, {{baseType}} right) => left.Equals(right) == false;
            """
        );

        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public static bool operator ==({{option}} left, {{someType}} right) => left.Equals(right);
                public static bool operator !=({{option}} left, {{someType}} right) => left.Equals(right) == false;
                """
            );
        }

        sb.AppendInterpolatedLine(
            $$"""
            public static bool operator ==({{option}} left, {{someTypeNullable}} right) => left.Equals(right);
            public static bool operator !=({{option}} left, {{someTypeNullable}} right) => left.Equals(right) == false;
            """
        );

        if (context.Configuration.IncludeOptionSomeObject is true) {
            if (context.Some.IsStruct) {
                sb.AppendInterpolatedLine(
                    $$"""
                    public static bool operator ==({{option}} left, {{BaseOption}}.Some<{{someType}}> right) => left.Equals(right);
                    public static bool operator !=({{option}} left, {{BaseOption}}.Some<{{someType}}> right) => left.Equals(right) == false;
                    """
                );
            }

            sb.AppendInterpolatedLine(
                $$"""
                public static bool operator ==({{option}} left, {{BaseOption}}.Some<{{someTypeNullable}}> right) => left.Equals(right);
                public static bool operator !=({{option}} left, {{BaseOption}}.Some<{{someTypeNullable}}> right) => left.Equals(right) == false;
                """
            );
        }
    }

    void WriteCastingMethods() {
        sb.AppendInterpolatedLine($"public {baseType} AsBase() => this;");
        if (context.Configuration.AddCastByRefMethod is true) {
            sb.AppendInterpolatedLine(
                $$"""
                public TOther CastByRef<TOther>() where TOther : struct, {{OptionMarker}}<{{context.Some.Type}}> =>
                    global::Perf.Holders.___HoldersInvisibleHelpers.CastOption<{{context.Option.DeclarationName}}, {{context.Some.Type}}, TOther>(in this);
                """
            );
        }
    }

    void WriteEqualityMethods() {
        sb.AppendInterpolatedLine(
            $$"""
            public override bool Equals(object? obj) =>
                obj switch {
            """
        );
        sb.Indent += 2;
        sb.AppendInterpolatedLine($"null => false,");
        sb.AppendInterpolatedLine($"{context.Option.DeclarationName} o1 => Equals(o1),");
        sb.AppendInterpolatedLine($"{baseType} o2 => Equals(o2),");
        sb.AppendInterpolatedLine($"{context.Some.Type} o3 => Equals(o3),");

        if (context.Configuration.IncludeOptionSomeObject is true) {
            sb.AppendInterpolatedLine($"{BaseOption}.Some<{context.Some.TypeNullable}> o4 => Equals(o4),");
            if (context.Some.IsStruct) {
                sb.AppendInterpolatedLine($"{BaseOption}.Some<{context.Some.Type}> o5 => Equals(o5),");
            }
        }

        sb.AppendInterpolatedLine($"{BaseOption}.None => {context.IsSome.Property} == false,");
        sb.AppendLine("_ => false");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;

        sb.AppendInterpolatedLine(
            $$"""
            public bool Equals({{context.Option.DeclarationName}} other) =>
                (state, other.state) switch {
                    ({{enumSome}}, {{enumSome}}) => {{context.Some.Field}}.Equals(other.{{context.Some.Field}}),
                    ({{enumNone}}, {{enumNone}}) => true,
                    _ => false
                };
            public bool Equals({{BaseOption}}<{{context.Some.Type}}> other) => other.Equals(this.AsBase());
            public bool Equals({{context.Some.TypeNullable}} v) => {{context.IsSome.Property}} && {{EqualityComparer}}<{{context.Some.TypeNullable}}>.Default.Equals({{context.Some.Field}}, v);
            """
        );
        if (context.Configuration.IncludeOptionSomeObject is true) {
            sb.AppendInterpolatedLine(
                $"public bool Equals({BaseOption}.Some<{context.Some.TypeNullable}> someObject) => {context.IsSome.Property} && {EqualityComparer}<{context.Some.TypeNullable}>.Default.Equals({context.Some.Field}, someObject.Value);"
            );
        }

        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine($"public bool Equals({context.Some.Type} v) => {context.IsSome.Property} && {context.Some.Field}.Equals(v);");
            if (context.Configuration.IncludeOptionSomeObject is true) {
                sb.AppendInterpolatedLine(
                    $"public bool Equals({BaseOption}.Some<{context.Some.Type}> someObject) => {context.IsSome.Property} && {context.Some.Field}.Equals(someObject.Value);"
                );
            }
        }
    }

    void WriteGetHashCode() {
        sb.AppendInterpolatedLine(
            $$"""
            public override int GetHashCode() =>
                state switch {
                    {{enumSome}} => {{context.Some.Field}}.GetHashCode(),
                    {{enumNone}} => {{BaseOption}}.None.Value.GetHashCode(),
                    _ => 0
                };
            """
        );
    }

    void WriteToString() {
        var resultString = compInfo.SupportNullableAnnotation() ? "string?" : "string";
        sb.AppendInterpolatedLine(
            $$"""
            public override {{resultString}} ToString() =>
                state switch {
                    {{enumSome}} => {{context.Some.Field}}.ToString(),
                    {{enumNone}} => {{BaseOption}}.None.Value.ToString(),
                    _ => ""
                };
            """
        );
    }

    void WriteDebugPrint() {
        sb.AppendInterpolatedLine(
            $$"""
            string DebugPrint() =>
                state switch {
                    {{enumSome}} => $"{{context.Some.Property}}={{{context.Some.Field}}}",
                    {{enumNone}} => "{{context.IsSome.Property}}",
                    _ => "Default"
                };
            """
        );
    }

    void WriteMapMethods() {
        sb.AppendInterpolatedLine(
            $$"""
            public {{BaseOption}}<TNew> Map<TNew>(global::System.Func<{{context.Some.Type}}, TNew> mapSome) where TNew : notnull =>
                {{context.IsSome.Property}} ? mapSome({{context.Some.Field}}) : default({{BaseOption}}<TNew>);
            public async global::System.Threading.Tasks.ValueTask<{{BaseOption}}<TNew>> Map<TNew>(
                global::System.Func<{{context.Some.Type}}, global::System.Threading.Tasks.ValueTask<TNew>> mapSome
            ) where TNew : notnull => {{context.IsSome.Property}} ? await mapSome({{context.Some.Field}}) : default({{BaseOption}}<TNew>);
            """
        );
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
