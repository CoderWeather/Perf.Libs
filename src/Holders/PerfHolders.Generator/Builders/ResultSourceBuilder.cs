namespace Perf.Holders.Generator.Builders;

using Internal;
using Types;

sealed class ResultSourceBuilder(
    ResultHolderContextInfo contextInfo,
    CompInfo compInfo
) {
    const string BaseResult = "global::Perf.Holders.Result";
    const string ResultMarker = "global::Perf.Holders.IResultHolder";
    const string Exceptions = "global::Perf.Holders.Exceptions.ResultHolderExceptions";
    const string DebuggerBrowsableNever = "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]";
    const string EditorBrowsable = "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";
    const string EqualityComparer = "global::System.Collections.Generic.EqualityComparer";

    readonly string baseType = $"{BaseResult}<{contextInfo.Ok.Type}, {contextInfo.Error.Type}>";

    readonly string throwDefault = $"throw {Exceptions}.Default<{contextInfo.Result.DeclarationName}, {contextInfo.Ok.Type}, {contextInfo.Error.Type}>()";

    readonly string enumState = $"{contextInfo.Result.OnlyName}State";
    readonly string enumStateOk = $"{contextInfo.Result.OnlyName}State.{contextInfo.Ok.Property}";
    readonly string enumStateError = $"{contextInfo.Result.OnlyName}State.{contextInfo.Error.Property}";
    readonly string enumStateDefault = $"{contextInfo.Result.OnlyName}State.Default";

    readonly InterpolatedStringBuilder sb = new(stringBuilder: new(12000));
    ResultHolderContextInfo context = contextInfo;

    // minimum at 1 because of generated type braces
    int bracesToCloseOnEnd = 1;

    void Preparation() {
        if (compInfo.SupportNullableAnnotation() is false) {
            if (context.Ok.IsStruct is false) {
                context = context with {
                    Ok = context.Ok with {
                        TypeNullable = context.Ok.Type
                    }
                };
            }

            if (context.Error.IsStruct is false) {
                context = context with {
                    Error = context.Error with {
                        TypeNullable = context.Error.Type
                    }
                };
            }
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

        var typeArgs = context switch {
            { Ok.IsTypeParameter: true, Error.IsTypeParameter: true } => $"<{context.Ok.Type}, {context.Error.Type}>",
            { Ok.IsTypeParameter: true }                              => $"<{context.Ok.Type}>",
            { Error.IsTypeParameter: true }                           => $"<{context.Error.Type}>",
            _                                                         => ""
        };
        var typeArgsConstraints = context switch {
            { Ok.IsTypeParameter: true, Error.IsTypeParameter: true } => $"where {context.Ok.Type} : notnull where {context.Error.Type} : notnull",
            { Ok.IsTypeParameter: true }                              => $"where {context.Ok.Type} : notnull",
            { Error.IsTypeParameter: true }                           => $"where {context.Error.Type} : notnull",
            _                                                         => ""
        };
        sb.AppendInterpolatedLine(
            $$"""
            sealed class {{context.Result.OnlyName}}_DebugView{{typeArgs}} {{typeArgsConstraints}}{
                public {{context.Result.OnlyName}}_DebugView({{context.Result.DeclarationName}} result) {
                    var stateField = typeof({{context.Result.DeclarationName}})
                        .GetField("state", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!;
                    this.State = stateField.GetValue(result)!;
                    this.Value = global::System.Enum.Format(stateField.FieldType, this.State, "G") switch {
                        "{{context.Ok.Property}}" => result.{{context.Ok.Property}},
                        "{{context.Error.Property}}" => result.{{context.Error.Property}},
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
            && context.Result.Accessibility is TypeAccessibility.Public
                ? "public "
                : "";

        sb.AppendInterpolatedLine(
            $$"""
            {{accessibilityModifier}}enum {{context.Result.OnlyName}}State : byte {
                Default = 0,
                {{context.Ok.Property}} = 1,
                {{context.Error.Property}} = 2
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
        var typeArg = context switch {
            { Ok.IsTypeParameter: true, Error.IsTypeParameter: true }       => "<,>",
            { Ok.IsTypeParameter: true } or { Error.IsTypeParameter: true } => "<>",
            _                                                               => ""
        };
        sb.AppendInterpolatedLine(
            $"""
            [global::System.Diagnostics.DebuggerTypeProxy(typeof({context.Result.OnlyName}_DebugView{typeArg}))]
            """
        );

        sb.AppendLine(
            """
            [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
            """
        );
        if (compInfo.SerializationSystemTextJsonAvailable) {
            sb.AppendInterpolatedLine(
                $"[global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::Perf.Holders.Serialization.SystemTextJson.ResultHolderJsonConverterFactory))]"
            );
        }

        if (compInfo.SerializationMessagePackAvailable) {
            sb.AppendInterpolatedLine(
                $"[global::MessagePack.MessagePackFormatterAttribute(typeof(global::Perf.Holders.Serialization.MessagePack.ResultHolderFormatterResolver))]"
            );
        }

        sb.AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]");
    }

    void WriteTypeDeclaration() {
        sb.AppendInterpolatedLine($"readonly partial struct {context.Result.DeclarationName} :");
        sb.Indent++;
        sb.AppendInterpolatedLine($"global::System.IEquatable<{context.Result.DeclarationName}>,");
        sb.AppendInterpolatedLine($"global::System.IEquatable<{baseType}>,");

        sb.AppendInterpolated($"global::System.IEquatable<{context.Ok.TypeNullable}>");
        if (context.Ok.IsStruct) {
            sb.AppendLine(",");
            sb.AppendInterpolated($"global::System.IEquatable<{context.Ok.Type}>");
        }

        if (context.Configuration.IncludeResultOkObject is true) {
            sb.AppendLine(",");
            sb.AppendInterpolated($"global::System.IEquatable<{BaseResult}.Ok<{context.Ok.Type}>>");
        }

        sb.AppendLine();

        if (context.Ok.IsTypeParameter) {
            sb.AppendInterpolatedLine($"where {context.Ok.Type} : notnull");
        }

        if (context.Error.IsTypeParameter) {
            sb.AppendInterpolatedLine($"where {context.Error.Type} : notnull");
        }

        sb.Indent--;
        sb.AppendLine("{");
        sb.Indent++;
    }

    void WriteConstructors() {
        var option = context.Result.OnlyName;
        var okType = context.Ok.Type;
        var okField = context.Ok.Field;
        var errorType = context.Error.Type;
        var errorField = context.Error.Field;

        sb.AppendInterpolatedLine(
            $$"""
            public {{option}}() {
                this.state = {{enumStateDefault}};
                this.{{okField}} = default!;
                this.{{errorField}} = default!;
            }
            public {{option}}({{okType}} {{okField}}) {
                this.state = {{enumStateOk}};
                this.{{okField}} = {{okField}};
                this.{{errorField}} = default!;
            }
            public {{option}}({{errorType}} {{errorField}}) {
                this.state = {{enumStateError}};
                this.{{okField}} = default!;
                this.{{errorField}} = {{errorField}};
            }
            """
        );
        if (context.Configuration.IncludeResultOkObject is true) {
            sb.AppendInterpolatedLine($"public {option}({BaseResult}.Ok<{okType}> okObject) : this({okField}: okObject.Value) {{ }}");
        }

        if (context.Configuration.IncludeResultErrorObject is true) {
            sb.AppendInterpolatedLine($"public {option}({BaseResult}.Error<{errorType}> errorObject) : this({errorField}: errorObject.Value) {{ }}");
        }
    }

    void WriteFields() {
        sb.AppendInterpolatedLine(
            $$"""
            readonly {{enumState}} state;
            readonly {{context.Ok.Type}} {{context.Ok.Field}};
            readonly {{context.Error.Type}} {{context.Error.Field}};
            """
        );
    }

    void WriteExplicitInterfaceMembers() {
        var marker = $"{ResultMarker}<{context.Ok.Type}, {context.Error.Type}>";

        if (context.Ok.Property != ResultHolderContextInfo.OkInfo.DefaultProperty) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                {{context.Ok.Type}} {{marker}}.Ok => {{context.Ok.Property}};
                """
            );
        }

        if (context.Error.Property != ResultHolderContextInfo.ErrorInfo.DefaultProperty) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                {{context.Error.Type}} {{marker}}.Error => {{context.Error.Property}};
                """
            );
        }

        if (context.IsOk.Property != ResultHolderContextInfo.IsOkInfo.DefaultProperty) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                bool {{marker}}.IsOk => {{context.IsOk.Property}};
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

        if (context.Ok.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial {{context.Ok.Type}} {{context.Ok.Property}} =>
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public {{context.Ok.Type}} {{context.Ok.Property}} =>
                """
            );
        }

        sb.Indent++;
        sb.AppendInterpolatedLine(
            $$"""
            state switch {
                {{enumStateOk}} => {{context.Ok.Field}},
                {{enumStateError}} => throw {{Exceptions}}.WrongAccess<{{context.Result.DeclarationName}}, {{context.Ok.Type}}, {{context.Error.Type}}>("{{context.Error.Property}}", "{{context.Ok.Property}}"),
                _ => {{throwDefault}}
            };
            """
        );
        sb.Indent--;

        if (context.Error.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial {{context.Error.Type}} {{context.Error.Property}} =>
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public {{context.Error.Type}} {{context.Error.Property}} =>
                """
            );
        }

        sb.Indent++;
        sb.AppendInterpolatedLine(
            $$"""
            state switch {
                {{enumStateOk}} => throw {{Exceptions}}.WrongAccess<{{context.Result.DeclarationName}}, {{context.Ok.Type}}, {{context.Error.Type}}>("{{context.Ok.Property}}", "{{context.Error.Property}}"),
                {{enumStateError}} => {{context.Error.Field}},
                _ => {{throwDefault}}
            };
            """
        );
        sb.Indent--;

        if (context.IsOk.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial bool {{context.IsOk.Property}} => state == {{enumStateOk}};
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public bool {{context.IsOk.Property}} => state == {{enumStateOk}};
                """
            );
        }
    }

    void WriteCastOperators() {
        var okField = context.Ok.Field;
        var okType = context.Ok.Type;
        var errorField = context.Error.Field;
        var errorType = context.Error.Type;
        var isOk = context.IsOk.Property;
        var result = context.Result.DeclarationName;

        if (context.Configuration.ImplicitCastOkTypeToResult is true) {
            sb.AppendInterpolatedLine(
                $$"""
                public static implicit operator {{result}}({{okType}} {{okField}}) => new({{okField}}: {{okField}});
                """
            );
        }

        if (context.Configuration.ImplicitCastErrorTypeToResult is true) {
            sb.AppendInterpolatedLine(
                $$"""
                public static implicit operator {{result}}({{errorType}} {{errorField}}) => new({{errorField}}: {{errorField}});
                """
            );
        }

        if (context.Configuration.IncludeResultOkObject is true) {
            sb.AppendInterpolatedLine($"public static implicit operator {result}({BaseResult}.Ok<{okType}> okObject) => new({okField}: okObject.Value);");
        }

        if (context.Configuration.IncludeResultErrorObject is true) {
            sb.AppendInterpolatedLine(
                $"public static implicit operator {result}({BaseResult}.Error<{errorType}> errorObject) => new({errorField}: errorObject.Value);"
            );
        }

        sb.AppendInterpolatedLine(
            $$"""
            public static implicit operator {{baseType}}({{result}} result) => result.{{isOk}} ? new(ok: result.{{okField}}) : new(error: result.{{errorField}});
            public static implicit operator {{result}}({{baseType}} result) => result.IsOk ? new({{okField}}: result.Ok) : new({{errorField}}: result.Error);
            public static implicit operator bool({{result}} result) => result.{{isOk}};
            """
        );
    }

    void WriteEqualityOperators() {
        var okType = context.Ok.Type;
        var errorType = context.Error.Type;
        var result = context.Result.DeclarationName;

        sb.AppendInterpolatedLine(
            $$"""
            public static bool operator ==({{result}} left, {{result}} right) => left.Equals(right);
            public static bool operator !=({{result}} left, {{result}} right) => left.Equals(right) is false;
            public static bool operator ==({{result}} left, {{baseType}} right) => left.Equals(right);
            public static bool operator !=({{result}} left, {{baseType}} right) => left.Equals(right) is false;
            public static bool operator ==({{result}} left, {{okType}} right) => left.Equals(right);
            public static bool operator !=({{result}} left, {{okType}} right) => left.Equals(right) is false;
            public static bool operator ==({{result}} left, {{errorType}} right) => left.Equals(right);
            public static bool operator !=({{result}} left, {{errorType}} right) => left.Equals(right) is false;
            """
        );
    }

    void WriteCastingMethods() {
        sb.AppendInterpolatedLine($"public {baseType} AsBase() => this;");
        if (context.Configuration.AddCastByRefMethod is true) {
            sb.AppendInterpolatedLine(
                $$"""
                public TOther CastByRef<TOther>() where TOther : struct, {{ResultMarker}}<{{context.Ok.Type}}, {{context.Error.Type}}> =>
                    global::Perf.Holders.___HoldersInvisibleHelpers.CastResult<{{context.Result.DeclarationName}}, {{context.Ok.Type}}, {{context.Error.Type}}, TOther>(in this);
                """
            );
        }
    }

    void WriteEqualityMethods() {
        var okType = context.Ok.Type;
        var okTypeForEquals = context.Ok.IsStruct ? context.Ok.Type : context.Ok.TypeNullable;
        var errorType = context.Error.Type;
        var errorTypeForEquals = context.Error.IsStruct ? context.Error.Type : context.Error.TypeNullable;

        sb.AppendInterpolatedLine(
            $$"""
            public override bool Equals(object? obj) =>
                obj switch {
            """
        );
        sb.Indent += 2;
        sb.AppendLine("null => false,");
        sb.AppendInterpolatedLine($"{context.Result.DeclarationName} o1 => Equals(o1),");
        sb.AppendInterpolatedLine($"{baseType} o2 => Equals(o2),");
        sb.AppendInterpolatedLine($"{okType} o3 => Equals(o3),");
        if (context.Configuration.IncludeResultOkObject is true) {
            sb.AppendInterpolatedLine($"{BaseResult}.Ok<{okType}> o4 => Equals(o4),");
        }

        sb.AppendInterpolatedLine($"{errorType} o5 => Equals(o5),");
        if (context.Configuration.IncludeResultErrorObject is true) {
            sb.AppendInterpolatedLine($"{BaseResult}.Error<{errorType}> o6 => Equals(o6),");
        }

        sb.AppendLine("_ => false");
        sb.Indent--;
        sb.AppendLine("};");
        sb.Indent--;

        sb.AppendInterpolatedLine(
            $$"""
            public bool Equals({{context.Result.DeclarationName}} other) =>
                (state, other.state) switch {
                    ({{enumStateOk}}, {{enumStateOk}}) => {{context.Ok.Field}}.Equals(other.{{context.Ok.Field}}),
                    ({{enumStateError}}, {{enumStateError}}) => {{context.Error.Field}}.Equals(other.{{context.Error.Field}}),
                    ({{enumStateDefault}}, {{enumStateDefault}}) => true,
                    _ => false
                };
            """
        );
        sb.AppendInterpolatedLine(
            $$"""
            public bool Equals({{baseType}} other) => other.Equals(({{baseType}})this);
            public bool Equals({{okTypeForEquals}} other) => {{context.IsOk.Property}} && {{EqualityComparer}}<{{okTypeForEquals}}>.Default.Equals({{context.Ok.Field}}, other);
            public bool Equals({{errorTypeForEquals}} other) => {{context.IsOk.Property}} == false && {{EqualityComparer}}<{{errorTypeForEquals}}>.Default.Equals({{context.Error.Field}}, other);
            """
        );
        if (context.Configuration.IncludeResultOkObject is true) {
            sb.AppendInterpolatedLine(
                $"public bool Equals({BaseResult}.Ok<{okType}> okObject) => {context.IsOk.Property} && {EqualityComparer}<{okTypeForEquals}>.Default.Equals({context.Ok.Field}, okObject.Value);"
            );
        }

        if (context.Configuration.IncludeResultErrorObject is true) {
            sb.AppendInterpolatedLine(
                $"public bool Equals({BaseResult}.Error<{errorType}> errorObject) => {context.IsOk.Property} == false && {EqualityComparer}<{errorTypeForEquals}>.Default.Equals({context.Error.Field}, errorObject.Value);"
            );
        }

        if (context.Ok.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public bool Equals({{context.Ok.TypeNullable}} other) => {{context.IsOk.Property}} && {{EqualityComparer}}<{{context.Ok.TypeNullable}}>.Default.Equals({{context.Ok.Field}}, other);
                """
            );
        }
    }

    void WriteGetHashCode() {
        sb.AppendInterpolatedLine(
            $$"""
            public override int GetHashCode() =>
                state switch {
                    {{enumStateOk}} => {{context.Ok.Field}}.GetHashCode(),
                    {{enumStateError}} => {{context.Error.Field}}.GetHashCode(),
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
                    {{enumStateOk}} => {{context.Ok.Field}}.ToString(),
                    {{enumStateError}} => {{context.Error.Field}}.ToString(),
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
                    {{enumStateOk}} => $"{{context.Ok.Property}}={{{context.Ok.Field}}}",
                    {{enumStateError}} => $"{{context.Error.Property}}={{{context.Error.Field}}}",
                    _ => "Default"
                };
            """
        );
    }

    void WriteMapMethods() {
        var okType = context.Ok.Type;
        var okField = context.Ok.Field;
        var errorType = context.Error.Type;
        var errorField = context.Error.Field;
        var isOk = context.IsOk.Property;

        sb.AppendInterpolatedLine(
            $$"""
            public {{BaseResult}}<TNewOk, {{errorType}}> Map<TNewOk>(Func<{{okType}}, TNewOk> mapOk)
                where TNewOk : notnull
                => {{isOk}} ? mapOk({{okField}}) : {{errorField}};
            public async global::System.Threading.Tasks.ValueTask<{{BaseResult}}<TNewOk, {{errorType}}>> Map<TNewOk>(Func<{{okType}}, global::System.Threading.Tasks.ValueTask<TNewOk>> mapOk)
                where TNewOk : notnull
                => {{isOk}} ? await mapOk({{okField}}) : {{errorField}};

            public {{BaseResult}}<{{okType}}, TNewError> MapError<TNewError>(Func<{{errorType}}, TNewError> mapError)
                where TNewError : notnull
                => {{isOk}} ? {{okField}} : mapError({{errorField}});
            public async global::System.Threading.Tasks.ValueTask<{{BaseResult}}<{{okType}}, TNewError>> MapError<TNewError>(Func<{{errorType}}, global::System.Threading.Tasks.ValueTask<TNewError>> mapError)
                where TNewError : notnull
                => {{isOk}} ? {{okField}} : await mapError({{errorField}});

            public {{BaseResult}}<TNewOk, TNewError> Map<TNewOk, TNewError>(
                Func<{{okType}}, TNewOk> mapOk,
                Func<{{errorType}}, TNewError> mapError
            ) where TNewOk : notnull where TNewError : notnull =>
                {{isOk}} ? mapOk({{okField}}) : mapError({{errorField}});
            public async global::System.Threading.Tasks.ValueTask<{{BaseResult}}<TNewOk, TNewError>> Map<TNewOk, TNewError>(
                Func<{{okType}}, global::System.Threading.Tasks.ValueTask<TNewOk>> mapOk,
                Func<{{errorType}}, global::System.Threading.Tasks.ValueTask<TNewError>> mapError
            ) where TNewOk : notnull where TNewError : notnull =>
                {{isOk}} ? await mapOk({{okField}}) : await mapError({{errorField}});
            """
        );
    }

    void WriteEndOfFile() {
        sb.Indent--;
        for (var i = 0; i < bracesToCloseOnEnd; i++) {
            sb.Append('}');
            if (i == bracesToCloseOnEnd - 1) {
                sb.AppendLine();
            }
        }
    }
}
