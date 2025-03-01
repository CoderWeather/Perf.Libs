namespace Perf.SourceGeneration;

file readonly record struct Wrapper(INamedTypeSymbol Type) {
    public readonly HashSet<INamedTypeSymbol> OriginTypes = new(SymbolEqualityComparer.Default);
    public readonly List<(ITypeSymbol Type, string Name)> Properties = new();
    public string[] OwnPropertyNames { get; init; } = Array.Empty<string>();
    public string?[] IgnoreMembers { get; init; } = Array.Empty<string>();
    public string?[] IncludeMembers { get; init; } = Array.Empty<string>();
    public bool SetMessagePackKeys { get; init; }
    public int StartMessagePackKey { get; init; }
}

[Generator]
public sealed class CopyTypeMembersGenerator : IIncrementalGenerator {
    private const string CopyTypeMembersAttributeFullName = "ExpressMobile.Services.Shared.Utilities.SourceGeneration.CopyTypeMembersAttribute";
    private const string IgnoreMembersAttributeFullName = "ExpressMobile.Services.Shared.Utilities.SourceGeneration.IgnoreMembersAttribute";
    private const string IncludeMembersAttributeFullName = "ExpressMobile.Services.Shared.Utilities.SourceGeneration.IncludeMembersAttribute";

    private const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";
    private const string MessagePackKeyAttributeFullName = "MessagePack.KeyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, ct) => {
                if (node is TypeDeclarationSyntax {
                    AttributeLists.Count: > 0
                } t) {
                    if (t.Modifiers.Any(SyntaxKind.PartialKeyword)) {
                        foreach (var al in t.AttributeLists) {
                            foreach (var a in al.Attributes) {
                                if (a.Name.ToString() is "CopyTypeMembers" or "CopyTypeMembersAttribute") {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (TypeDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
                    return default;
                }

                foreach (var a in symbol.GetAttributes()) {
                    if (a.AttributeClass?.FullPath() is CopyTypeMembersAttributeFullName) {
                        return symbol;
                    }
                }

                return default;
            }
        );
        var filtered = types.Where(x => x != default).Select((x, _) => x!);

        context.RegisterSourceOutput(
            filtered.Collect().Combine(context.CompilationProvider),
            static (context, tuple) => {
                var (types, compilation) = tuple;
                if (types.IsDefaultOrEmpty) {
                    return;
                }

                var ct = context.CancellationToken;

                var wrappers = new Dictionary<INamedTypeSymbol, Wrapper>(SymbolEqualityComparer.Default);

                foreach (var type in types) {
                    ct.ThrowIfCancellationRequested();

                    var markers = type.GetAttributes(CopyTypeMembersAttributeFullName);
                    var setMessagePackKeys = false;
                    if (type.TryGetAttribute(MessagePackObjectAttributeFullName) is { } msgPackObj) {
                        if (msgPackObj.ConstructorArguments.Length is 0 || msgPackObj.ConstructorArguments[0].As<bool>() is false) {
                            setMessagePackKeys = true;
                        } else if (msgPackObj.ConstructorArguments[0].As<bool>()) {
                            setMessagePackKeys = false;
                        }
                    }

                    var ignoreMembers = type.GetAttributes(IgnoreMembersAttributeFullName)
                       .SelectMany(x => x?.ConstructorArguments[0].Values.Select(y => y.Value as string))
                       .ToArray();
                    var includeMembers = type.GetAttributes(IncludeMembersAttributeFullName)
                       .SelectMany(x => x?.ConstructorArguments[0].Values.Select(y => y.Value as string))
                       .ToArray();

                    var typeProperties = type.GetMembers()
                       .OfType<IPropertySymbol>()
                       .Where(p => p is {
                                IsStatic: false,
                                IsIndexer: false,
                                DeclaredAccessibility: Accessibility.Public
                            }
                        )
                       .ToArray();

                    var startMessagePackKey = 0;
                    if (setMessagePackKeys) {
                        var withKeyAttr = typeProperties.Where(x => x.HasAttribute(MessagePackKeyAttributeFullName)).ToArray();
                        if (withKeyAttr.Any()) {
                            var maxIndex = withKeyAttr
                               .Select(x => x.GetAttribute(MessagePackKeyAttributeFullName).ConstructorArguments[0].As<int>())
                               .Max();
                            startMessagePackKey = maxIndex + 1;
                        }
                    }

                    var ownPropertyNames = typeProperties.Select(x => x.Name).ToArray();

                    var w = new Wrapper(type) {
                        OwnPropertyNames = ownPropertyNames,
                        IgnoreMembers = ignoreMembers,
                        IncludeMembers = includeMembers,
                        SetMessagePackKeys = setMessagePackKeys,
                        StartMessagePackKey = startMessagePackKey
                    };
                    wrappers[w.Type] = w;

                    foreach (var m in markers) {
                        ct.ThrowIfCancellationRequested();

                        var originType = m.ConstructorArguments[0].As<INamedTypeSymbol>()
                         ?? (m.ConstructorArguments[0].As<string>() is { } fullTypeName ? compilation.GetTypeByMetadataName(fullTypeName) : null)
                         ?? throw new InvalidOperationException();

                        var originProperties = originType.GetMembers()
                           .OfType<IPropertySymbol>()
                           .Where(x => w.Properties.Span().All(y => y.Name != x.Name))
                           .Where(p => p is {
                                          IsStatic: false,
                                          IsIndexer: false,
                                          DeclaredAccessibility: Accessibility.Public
                                      }
                                   && Enumerable.Contains(ownPropertyNames, p.Name) is false
                            )
                           .Where(x => {
                                    var result = true;
                                    if (ignoreMembers.Any()) {
                                        result &= Enumerable.Contains(ignoreMembers, x.Name) is false;
                                    }

                                    if (includeMembers.Any()) {
                                        result &= Enumerable.Contains(includeMembers, x.Name);
                                    }

                                    return result;
                                }
                            )
                           .Select(x => (x.Type, x.Name));

                        w.Properties.AddRange(originProperties);
                        w.OriginTypes.Add(originType);
                    }
                }

                var wrappersList = wrappers.Values;
                foreach (var pair in wrappers) {
                    var w = pair.Value;
                    foreach (var originType in w.OriginTypes) {
                        if (wrappers.TryGetValue(originType, out var other)) {
                            w.Properties.AddRange(
                                other.Properties.Where(x => {
                                        if (w.Properties.Span().All(y => y.Name != x.Name) is false) {
                                            return false;
                                        }

                                        if (Enumerable.Contains(w.OwnPropertyNames, x.Name)) {
                                            return false;
                                        }

                                        var result = true;

                                        if (w.IgnoreMembers.Any()) {
                                            result &= Enumerable.Contains(w.IgnoreMembers, x.Name) is false;
                                        }

                                        if (w.IncludeMembers.Any()) {
                                            result &= Enumerable.Contains(w.IncludeMembers, x.Name);
                                        }

                                        return result;
                                    }
                                )
                            );
                        }

                        for (var i = 0; i < w.Properties.Count; i++) {
                            var (t, n) = w.Properties[i];
                            var isArray = false;
                            if (t is IArrayTypeSymbol at) {
                                isArray = true;
                                t = at.ElementType;
                            }

                            var checkExistingCopiedMemberWithTarget = wrappersList.FirstOrDefault(x => x.OriginTypes.Any(y => y.StrictEquals(t)));
                            if (checkExistingCopiedMemberWithTarget != default) {
                                ITypeSymbol newType = checkExistingCopiedMemberWithTarget.Type;
                                if (isArray) {
                                    newType = compilation.CreateArrayTypeSymbol(newType);
                                }

                                if (t.NullableAnnotation is NullableAnnotation.Annotated) {
                                    newType = (INamedTypeSymbol)newType.WithNullableAnnotation(NullableAnnotation.Annotated);
                                }

                                w.Properties[i] = (newType, n);
                            }
                        }
                    }
                }

                foreach (var group in wrappers.GroupBy(x => x.Key.ContainingNamespace.ToDisplayString())) {
                    ct.ThrowIfCancellationRequested();
                    var ns = group.Key!;

                    using var writer = new IndentedTextWriter(new StringWriter(), "	");
                    writer.WriteLines("// <auto-generated />", null, "#nullable enable");
                    writer.WriteLine($"namespace {ns} {{");
                    writer.Indent++;

                    foreach (var pair in group) {
                        var type = pair.Key;
                        var w = pair.Value;
                        if (w.Properties.Any() is false) {
                            continue;
                        }

                        writer.WriteLine($"partial {type.DeclarationString()} {type.MinimalName()} {{");
                        writer.Indent++;

                        var keyIndex = w.StartMessagePackKey;
                        foreach (var p in w.Properties) {
                            if (w.SetMessagePackKeys) {
                                writer.WriteLine($"[global::MessagePack.Key({keyIndex})]");
                                keyIndex++;
                            }

                            var defaultString = p.Type switch {
                                IArrayTypeSymbol {
                                        NullableAnnotation: not NullableAnnotation.Annotated
                                    } at => $"System.Array.Empty<{at.ElementType.GlobalName()}>()",
                                { IsReferenceType: true } => "null!",
                                _                         => "default"
                            };

                            writer.WriteLine($"public {p.Type.GlobalName()} {p.Name} {{ get; set; }} = {defaultString};");
                        }

                        writer.Indent--;
                        writer.WriteLine("}");
                    }

                    writer.Indent--;
                    writer.WriteLine("}");

                    ct.ThrowIfCancellationRequested();
                    var sourceCode = writer.InnerWriter.ToString()!;
                    context.AddSource($"{GetShortNamespaceName(compilation, ns)}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
        );
    }

    private static string GetShortNamespaceName(Compilation compilation, string ns) {
        var shorted = ns.Replace($"{compilation.Assembly.Name}.", null);
        return shorted.Length > 0 ? shorted : "Generated";
    }
}
