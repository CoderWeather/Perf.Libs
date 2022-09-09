namespace Perf.SourceGeneration.Monads;

[Generator]
public sealed class ResultMonadGenerator : IIncrementalGenerator {
    private const string InterfaceMarkerFullName = "Perf.Utilities.Monads.IResultMonad";

    private const string Pattern = @"
// <auto-generated />
#nullable enable

namespace {4};

using Perf.Utilities.Monads;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
readonly partial struct {3} : IEquatable<{3}>, IEquatable<Result<{1}, {2}>> {{
	public {0}() => (ok, error, init, isOk) = (default!, default!, false, false);
	public {0}({1} ok) => (this.ok, error, init, isOk) = (ok, default!, true, true);
	public {0}(Result.Ok<{1}> ok) => (this.ok, error, init, isOk) = (ok.Value, default!, true, true);
	public {0}({2} error) => (ok, this.error, init, isOk) = (default!, error, true, false);
	public {0}(Result.Error<{2}> error) => (ok, this.error, init, isOk) = (default!, error.Value, true, false);
	private readonly bool init;
	private readonly bool isOk;
	private readonly {1} ok;
	private readonly {2} error;
	public {1} Ok => init ? isOk ? ok : throw new InvalidOperationException($""Cannot access Ok. {5} is Error"") : throw new InvalidOperationException($""{5} is Empty"");
	{1} IResultMonad<{1}, {2}>.Ok => Ok;
	public {2} Error => init ? isOk is false ? error : throw new InvalidOperationException($""Cannot access Error. {5} is Ok"") : throw new InvalidOperationException($""{5} is Empty"");
	{2} IResultMonad<{1}, {2}>.Error => Error;
	public bool IsOk => init ? isOk : throw new InvalidOperationException($""{5} is Empty"");
	bool IResultMonad<{1}, {2}>.IsOk => IsOk;
	public static implicit operator {3}({1} ok) => new(ok: ok);
	public static implicit operator {3}(Result.Ok<{1}> ok) => new(ok: ok.Value);
	public static implicit operator {3}({2} error) => new(error: error);
	public static implicit operator {3}(Result.Error<{2}> error) => new(error: error.Value);
	public static implicit operator Result<{1}, {2}>({3} m) => m.IsOk ? new(ok: m.ok) : new(error: m.error);
	public static implicit operator {3}(Result<{1}, {2}> r) => r.IsOk ? new(ok: r.Ok) : new(error: r.Error);
	public bool Equals({3} other) =>
		IsOk && other.IsOk && EqualityComparer<{1}>.Default.Equals(ok, other.ok)
	 || IsOk is false && other.IsOk is false && EqualityComparer<{2}>.Default.Equals(error, other.error);
	public bool Equals(Result<{1}, {2}> other) => other.Equals((Result<{1}, {2}>)this);
	public override bool Equals(object? obj) => obj is {3} other && Equals(other);
	public override int GetHashCode() => IsOk ? ok.GetHashCode() : error.GetHashCode();
}}
";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, ct) => {
                if (node is StructDeclarationSyntax {
                        BaseList.Types.Count: > 0
                    } s
                   ) {
                    if (s.Modifiers.Any(SyntaxKind.PartialKeyword) && s.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)) {
                        foreach (var bt in s.BaseList.Types) {
                            if (bt is {
                                    Type: GenericNameSyntax {
                                        Identifier.Text: "IResultMonad",
                                        TypeArgumentList.Arguments.Count: 2
                                    }
                                }) {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is { } symbol) {
                    var hasOneInterface = false;
                    foreach (var i in symbol.Interfaces) {
                        if (i.FullPath() is InterfaceMarkerFullName) {
                            if (hasOneInterface) {
                                return default;
                            }

                            hasOneInterface = true;
                        }
                    }

                    if (hasOneInterface) {
                        return symbol;
                    }
                }

                return default;
            }
        );
        var filtered = types.Where(static x => x != default).Select(static (x, _) => x!);

        context.RegisterSourceOutput(filtered,
            static (context, symbol) => {
                var i = symbol.Interfaces.First(x => x.FullPath() is InterfaceMarkerFullName);
                var arg1 = i.TypeArguments[0];
                var arg2 = i.TypeArguments[1];

                var sourceText = string.Format(Pattern,
                        symbol.Name,
                        arg1 is ITypeParameterSymbol ? arg1.Name : arg1.GlobalName(),
                        arg2 is ITypeParameterSymbol ? arg2.Name : arg2.GlobalName(),
                        symbol.MinimalName(),
                        symbol.ContainingNamespace.ToDisplayString(),
                        symbol.TypeArguments.Length switch {
                            0 => symbol.Name,
                            1 => $"{symbol.Name}<{{typeof({symbol.TypeArguments[0].MinimalName()}).Name}}>",
                            2 =>
                                $"{symbol.Name}<{{typeof({symbol.TypeArguments[0].MinimalName()}).Name}}, {{typeof({symbol.TypeArguments[1].MinimalName()}).Name}}>",
                            _ => symbol.MinimalName()
                        }
                    )
                   .Trim();

                context.AddSource($"{MinimalNameWithGenericMetadata(symbol)}.g.cs", SourceText.From(sourceText, Encoding.UTF8));

                static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
                    return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
                }
            });
    }
}