// ReSharper disable NotAccessedPositionalProperty.Local

namespace Perf.ValueObjects.Generator;

[Generator]
sealed class ValueObjectGenerator : IIncrementalGenerator {
    const string GeneralFullPath = "Perf.ValueObjects.IValueObject";
    const string ValidatableFullPath = "Perf.ValueObjects.IValidatableValueObject";

    enum ValueObjectType { Undefined = 0, General = 1, Validatable = 2 }

    readonly record struct ObjectInfo(
        string FullName,
        ValueObjectType ContractType,
        Dictionary<string, string?> PatternValues
    ) {
        public bool Equals(ObjectInfo other) {
            if (ContractType != other.ContractType) {
                return false;
            }

            if (FullName.AsSpan().SequenceEqual(other.FullName.AsSpan()) is false) {
                return false;
            }

            var values = PatternValues;
            var otherValues = other.PatternValues;
            if (values == null! || otherValues == null!) {
                return false;
            }

            if (values.Count != otherValues.Count) {
                return false;
            }

            foreach (var p in values) {
                var k = p.Key;
                var value = p.Value;
                if (otherValues.TryGetValue(k, out var otherValue) is false || value.AsSpan().SequenceEqual(otherValue.AsSpan()) is false) {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() {
            HashCode hc = default;
            hc.Add(ContractType);
            foreach (var p in PatternValues) {
                hc.Add(p.Key);
                hc.Add(p.Value);
            }

            return hc.ToHashCode();
        }
    }

    readonly record struct CompInfo(LanguageVersion? Version);

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => {
                if (node is not TypeDeclarationSyntax {
                        Modifiers.Count: > 0,
                        BaseList.Types.Count: > 0,
                        TypeParameterList: null
                    } tds
                    || tds.Modifiers.Any(SyntaxKind.PartialKeyword) is false
                    || tds.Modifiers.Any(SyntaxKind.RefKeyword)
                ) {
                    return false;
                }

                // for classic record keyword=record, class=class
                // for record struct keyword=record and only in RecordDeclarationSyntax additional keyword=struct
                if (node is not StructDeclarationSyntax and not RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: (int)SyntaxKind.StructKeyword }) {
                    return false;
                }

                foreach (var bt in tds.BaseList.Types) {
                    switch (bt) {
                        case SimpleBaseTypeSyntax {
                            Type: QualifiedNameSyntax {
                                Right: GenericNameSyntax {
                                    Identifier.Text: "IValueObject" or "IValidatableValueObject",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                        }:
                        case {
                            Type: GenericNameSyntax {
                                Identifier.Text: "IValueObject" or "IValidatableValueObject",
                                TypeArgumentList.Arguments.Count: 1
                            }
                        }:
                            return true;
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (TypeDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
                    return null;
                }

                INamedTypeSymbol marker = null!;
                ValueObjectType type = default;
                foreach (var i in symbol.Interfaces) {
                    switch (i.FullPath().AsSpan()) {
                        case GeneralFullPath:
                            type = ValueObjectType.General;
                            break;
                        case ValidatableFullPath:
                            type = ValueObjectType.Validatable;
                            break;
                        default: continue;
                    }

                    if (marker is not null) {
                        return null;
                    }

                    marker = i;
                }

                if (type == default) {
                    return null;
                }

                var arg = marker.TypeArguments[0];

                var ns = symbol.ContainingNamespace.ToDisplayString();
                var typeName = symbol.Name;
                var qualifiedArg = arg.GlobalName();
                var qualifiedArgForEquals = arg switch {
                    { IsReferenceType: true } => $"{qualifiedArg}?", // TODO check c# nullable support
                    _                         => qualifiedArg
                };
                var typeKeyword = symbol switch {
                    { IsValueType: true, IsRecord: true } => "record struct",
                    { IsValueType: true }                 => "struct",
                    _                                     => throw new InvalidOperationException($"Unsupported object type: {symbol.GlobalName()}")
                };
                var argHashCodeAccess = arg switch {
                    { IsReferenceType: true } => "value?.GetHashCode() ?? 0",
                    { IsValueType: true }     => "value.GetHashCode()",
                    _                         => throw new InvalidOperationException($"Unsupported object type: {symbol.GlobalName()}")
                };
                var argToStringAccess = arg switch {
                    { IsReferenceType: true } => "value?.ToString()",
                    { IsValueType: true }     => "value.ToString()",
                    _                         => throw new InvalidOperationException($"Unsupported object type: {symbol.GlobalName()}")
                };

                return (ObjectInfo?)new ObjectInfo(
                    FullName: symbol.FullPath(),
                    ContractType: type,
                    PatternValues: new Dictionary<string, string?> {
                        ["Namespace"] = ns,
                        ["TypeKeyword"] = typeKeyword,
                        ["Name"] = typeName,
                        ["QualifiedValue"] = qualifiedArg,
                        ["QualifiedValueForEquals"] = qualifiedArgForEquals,
                        ["ValueHashCodeAccess"] = argHashCodeAccess,
                        ["ValueToStringAccess"] = argToStringAccess
                    }
                );
            }
        );

        var filtered = types
            .Where(x => x != null)
            .Select((x, _) => x!.Value);

        var compInfo = context.CompilationProvider
            .Select(static (c, _) => {
                    LanguageVersion? langVersion = c is CSharpCompilation comp ? comp.LanguageVersion : null;
                    return new CompInfo(langVersion);
                }
            );

        var typesWithCompInfo = filtered.Combine(compInfo);

        context.RegisterSourceOutput(
            typesWithCompInfo,
            static (context, source) => {
                var (objInfo, compInfo) = source;

                objInfo.PatternValues["DebugViewVisibility"] = compInfo.Version is >= LanguageVersion.CSharp11
                    ? "file "
                    : "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n";

                string sourceText;
                if (objInfo.ContractType is ValueObjectType.General) {
                    sourceText = PatternFormatter.Format(GeneralPattern, objInfo.PatternValues);
                } else if (objInfo.ContractType is ValueObjectType.Validatable) {
                    sourceText = PatternFormatter.Format(ValidatablePattern, objInfo.PatternValues);
                } else {
                    throw new InvalidOperationException("Unsupported ValueObject contract to process source generation");
                }

                context.AddSource($"{objInfo.FullName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        );
    }

    const string GeneralPattern = """
        // <auto-generated/>
        #nullable enable

        namespace {Namespace};

        {DebugViewVisibility}sealed class {Name}_DebugView {
            public {Name}_DebugView({Name} value) {
                try {
                    Value = value.Value;
                } catch {
                    Value = "!!! Incorrect State !!!";
                }
            }

            public object? Value { get; }
        }

        [global::System.Diagnostics.DebuggerTypeProxy(typeof({Name}_DebugView))]
        [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        readonly partial {TypeKeyword} {Name} {
            public {Name}() {
                init = false;
                value = default!;
            }
            
            public {Name}({QualifiedValue} value) {
                init = true;
                this.value = value;
                // TODO Validation moves
            }

            readonly bool init;
            readonly {QualifiedValue} value;
            [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {QualifiedValue} Value => init ? value : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();

            public static implicit operator {QualifiedValue}({Name} vo) => vo.Value;
            public static explicit operator {Name}({QualifiedValue} value) => new(value);
            public override string? ToString() => init ? {ValueToStringAccess} : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();
            public override int GetHashCode() => init ? {ValueHashCodeAccess} : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();

            string DebugPrint() => init ? $"Value={value}" : "!!! Incorrect State !!!";
        }
        """;

    const string ValidatablePattern = """
        // <auto-generated/>
        #nullable enable

        namespace {Namespace};

        {DebugViewVisibility}sealed class {Name}_DebugView {
            public {Name}_DebugView({Name} value) {
                try {
                    Value = value.Value;
                } catch {
                    Value = "!!! Incorrect State !!!";
                }
            }

            public object? Value { get; }
        }

        [global::System.Diagnostics.DebuggerTypeProxy(typeof({Name}_DebugView))]
        [global::System.Diagnostics.DebuggerDisplay("{DebugPrint()}")]
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        readonly partial {TypeKeyword} {Name} {
            public {Name}() {
                init = false;
                value = default!;
            }
            
            public {Name}({QualifiedValue} value) {
                init = true;
                this.value = value;
                if (Validate(value) is false) throw global::Perf.ValueObjects.ValueObjectException.Validation<{Name}>(this);
            }

            readonly bool init;
            readonly {QualifiedValue} value;
            [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {QualifiedValue} Value => init ? value : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();

            public static implicit operator {QualifiedValue}({Name} vo) => vo.Value;
            public static explicit operator {Name}({QualifiedValue} value) => new(value);
            public override string? ToString() => init ? {ValueToStringAccess} : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();
            public override int GetHashCode() => init ? {ValueHashCodeAccess} : throw global::Perf.ValueObjects.ValueObjectException.Initialization<{Name}>();

            string DebugPrint() => init ? $"Value={value}" : "!!! Incorrect State !!!";
        }
        """;
}
