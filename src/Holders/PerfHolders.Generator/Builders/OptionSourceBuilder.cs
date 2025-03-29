namespace Perf.Holders.Generator.Builders;

using Internal;
using Types;

sealed class OptionSourceBuilder(
    OptionHolderContextInfo contextInfo,
    CompInfo compInfo
) {
    const string OptionState = "global::Perf.Holders.OptionState";
    const string BaseOption = "global::Perf.Holders.Option";
    const string OptionMarker = "global::Perf.Holders.IOptionHolder";
    const string Exceptions = "global::Perf.Holders.Exceptions.OptionHolderExceptions";
    const string DebuggerBrowsableNever = "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]";
    const string EditorBrowsable = "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";
    const string EqualityComparer = "global::System.Collections.Generic.EqualityComparer";

    readonly InterpolatedStringBuilder sb = new(stringBuilder: new(8500));
    OptionHolderContextInfo context = contextInfo;

    // minimum at 1 because of generated type braces
    int bracesToCloseOnEnd = 1;
    bool debugViewAdded;

    void Preparation() {
        if (compInfo.SupportNullableAnnotation() is false && context.Some.IsStruct is false) {
            context = context with {
                Some = context.Some with {
                    TypeNullable = context.Some.Type
                }
            };
        }
    }

    void WriteAll() {
        Preparation();
        DeclareTopLevelStatements();
        WriteDeclarationClasses();
        WriteDebugView();
        WriteTypeAttributes();
        WriteTypeDeclaration();
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

        debugViewAdded = true;
        var typeArgs = context.Some.IsTypeArgument ? $"<{context.Some.Type}>" : "";
        var typeArgsConstraints = context.Some.IsTypeArgument ? $"where {context.Some.Type} : notnull " : "";
        sb.AppendInterpolatedLine(
            $$"""
            sealed class {{context.Option.OnlyName}}_DebugView{{typeArgs}} {{typeArgsConstraints}}{
                public {{context.Option.OnlyName}}_DebugView({{context.Option.DeclarationName}} opt) {
                    this.State = ({{OptionState}})typeof({{context.Option.DeclarationName}})
                        .GetField("state", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!
                        .GetValue(opt)!;
                    this.Value = this.State switch {
                        {{OptionState}}.Some => opt.{{context.Some.Property}},
                        {{OptionState}}.None => "None",
                        _ => "!!! Incorrect State !!!"
                    };
                }
                public {{OptionState}} State { get; }
                public object? Value { get; }
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
        if (debugViewAdded) {
            var typeArg = context.Some.IsTypeArgument ? "<>" : "";
            sb.AppendInterpolatedLine(
                $"""
                [global::System.Diagnostics.DebuggerTypeProxy(typeof({context.Option.OnlyName}_DebugView{typeArg}))]
                """
            );
        }

        sb.AppendLine(
            """
            [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
            """
        );
        sb.AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]");
    }

    void WriteTypeDeclaration() {
        sb.AppendInterpolated($"readonly partial struct {context.Option.DeclarationName} :");
        sb.Indent++;
        sb.AppendInterpolatedLine($"global::System.IEquatable<{context.Option.DeclarationName}>,");
        sb.AppendInterpolatedLine($"global::System.IEquatable<{BaseOption}<{context.Some.Type}>>,");

        sb.AppendInterpolatedLine($"global::System.IEquatable<{context.Some.TypeNullable}>,");
        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine($"global::System.IEquatable<{context.Some.Type}>,");
        }

        sb.AppendInterpolated($"global::System.IEquatable<{BaseOption}.Some<{context.Some.TypeNullable}>>");
        if (context.Some.IsStruct) {
            sb.AppendLine(",");
            sb.AppendInterpolatedLine($"global::System.IEquatable<{BaseOption}.Some<{context.Some.Type}>>");
        }

        if (context.Some.IsTypeArgument) {
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
                this.state = {{OptionState}}.None;
                this.{{field}} = default!;
            }
            """
        );
        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{someType}} {{field}}) {
                    this.state = {{OptionState}}.Some;
                    this.{{field}} = {{field}};
                }
                """
            );
            sb.AppendInterpolatedLine(
                $$"""
                public {{option}}({{someTypeNullable}} {{field}}) {
                    if ({{field}} != null) {
                        this.state = {{OptionState}}.Some;
                        this.{{field}} = {{field}}!.Value;
                    } else {
                        this.state = {{OptionState}}.None;
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
                        this.state = {{OptionState}}.Some;
                        this.{{field}} = {{field}}!;
                    } else {
                        this.state = {{OptionState}}.None;
                        this.{{field}} = default!;
                    }
                }
                """
            );
        }

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

    void WriteFields() {
        sb.AppendInterpolatedLine(
            $$"""
            private readonly {{OptionState}} state;
            private readonly {{context.Some.Type}} {{context.Some.Field}};
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
                {{OptionState}}.Some => {{context.Some.Field}},
                {{OptionState}}.None => throw {{Exceptions}}.SomeAccessWhenNone<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(),
                _ => throw {{Exceptions}}.StateOutOfValidValues<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(state)
            };
            """
        );
        sb.Indent--;

        if (context.IsSome.HavePartial) {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public partial bool {{context.IsSome.Property}} =>
                """
            );
        } else {
            sb.AppendInterpolatedLine(
                $$"""
                {{DebuggerBrowsableNever}}
                public bool {{context.IsSome.Property}} =>
                """
            );
        }

        sb.Indent++;

        sb.AppendInterpolatedLine(
            $$"""
            state switch {
                {{OptionState}}.Some => true,
                {{OptionState}}.None => false,
                _ => throw {{Exceptions}}.StateOutOfValidValues<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(state)
            };
            """
        );

        sb.Indent--;
    }

    void WriteCastOperators() {
        var field = context.Some.Field;
        var someType = context.Some.Type;
        var someTypeNullable = context.Some.TypeNullable;
        var option = context.Option.DeclarationName;

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

        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public static implicit operator {{option}}({{BaseOption}}.Some<{{someType}}> someObject) => new(someObject: someObject);
                """
            );
        }

        sb.AppendInterpolatedLine(
            $$"""
            public static implicit operator {{option}}({{BaseOption}}.Some<{{someTypeNullable}}> someObject) => new(someObject: someObject);
            public static implicit operator {{option}}({{BaseOption}}.None _) => default;
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
        var baseOption = $"{BaseOption}<{someType}>";

        sb.AppendInterpolatedLine(
            $$"""
            public static bool operator ==({{option}} left, {{option}} right) => left.Equals(right);
            public static bool operator !=({{option}} left, {{option}} right) => left.Equals(right) == false;
            public static bool operator ==({{option}} left, {{baseOption}} right) => left.Equals(right);
            public static bool operator !=({{option}} left, {{baseOption}} right) => left.Equals(right) == false;
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

    void WriteCastingMethods() {
        sb.AppendInterpolatedLine(
            $$"""
            public {{BaseOption}}<{{context.Some.Type}}> AsBase() => this;
            public TOther CastByRef<TOther>() where TOther : struct, {{OptionMarker}}<{{context.Some.Type}}> =>
                global::Perf.Holders.___HoldersInvisibleHelpers.CastOption<{{context.Option.DeclarationName}}, {{context.Some.Type}}, TOther>(in this);
            """
        );
    }

    void WriteEqualityMethods() {
        sb.AppendInterpolatedLine(
            $$"""
            public override bool Equals(object? obj) =>
                obj switch {
                    null => false,
                    {{context.Option.DeclarationName}} o1 => Equals(o1),
                    {{BaseOption}}<{{context.Some.Type}}> o2 => Equals(o2),
                    {{context.Some.Type}} o4 => Equals(o4),
                    {{BaseOption}}.Some<{{context.Some.TypeNullable}}> o5 => Equals(o5),{{
                        (context.Some.IsStruct ? $"{BaseOption}.Some<{context.Some.Type}> o6 => Equals(o6)," : null)}}
                    {{BaseOption}}.None => {{context.IsSome.Property}} == false,
                    _ => false
                };
            """
        );

        sb.AppendInterpolatedLine(
            $$"""
            public bool Equals({{context.Option.DeclarationName}} other) =>
                (state, other.state) switch {
                    ({{OptionState}}.Some, {{OptionState}}.Some) => {{context.Some.Field}}.Equals(other.{{context.Some.Field}}),
                    ({{OptionState}}.None, {{OptionState}}.None) => true,
                    _ => throw {{Exceptions}}.StateOutOfValidValues<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(state)
                };
            public bool Equals({{BaseOption}}<{{context.Some.Type}}> other) => other.Equals(this.AsBase());
            public bool Equals({{context.Some.TypeNullable}} v) => {{context.IsSome.Property}} && {{EqualityComparer}}<{{context.Some.TypeNullable}}>.Default.Equals({{context.Some.Field}}, v);
            public bool Equals({{BaseOption}}.Some<{{context.Some.TypeNullable}}> someObject) => {{context.IsSome.Property}} && {{EqualityComparer}}<{{context.Some.TypeNullable}}>.Default.Equals({{context.Some.Field}}, someObject.Value);
            """
        );
        if (context.Some.IsStruct) {
            sb.AppendInterpolatedLine(
                $$"""
                public bool Equals({{context.Some.Type}} v) => {{context.IsSome.Property}} && {{context.Some.Field}}.Equals(v);
                public bool Equals({{BaseOption}}.Some<{{context.Some.Type}}> someObject) => {{context.IsSome.Property}} && {{context.Some.Field}}.Equals(someObject.Value);
                """
            );
        }
    }

    void WriteGetHashCode() {
        sb.AppendInterpolatedLine(
            $$"""
            public override int GetHashCode() =>
                state switch {
                    {{OptionState}}.Some => {{context.Some.Field}}.GetHashCode(),
                    {{OptionState}}.None => {{OptionState}}.None.GetHashCode(),
                    _ => throw {{Exceptions}}.StateOutOfValidValues<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(state)
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
                    {{OptionState}}.Some => {{context.Some.Field}}.ToString(),
                    {{OptionState}}.None => {{OptionState}}.None.ToString(),
                    _ => throw {{Exceptions}}.StateOutOfValidValues<{{context.Option.DeclarationName}}, {{context.Some.Type}}>(state)
                };
            """
        );
    }

    void WriteDebugPrint() {
        sb.AppendInterpolatedLine(
            $$"""
            string DebugPrint() =>
                state switch {
                    {{OptionState}}.Some => $"{{context.Some.Property}}={{{context.Some.Field}}}",
                    {{OptionState}}.None => "None",
                    _ => "!!! Incorrect State !!!"
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

    void WriteEndOfFile() {
        sb.Indent--;
        for (var i = 0; i < bracesToCloseOnEnd; i++) {
            sb.Append('}');
            if (i == bracesToCloseOnEnd - 1) {
                sb.AppendLine();
            }
        }
    }

    public string WriteAllAndBuild() {
        WriteAll();
        var result = sb.ToString();
        sb.Clear();
        return result;
    }
}
