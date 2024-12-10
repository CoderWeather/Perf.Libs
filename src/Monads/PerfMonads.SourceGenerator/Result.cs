namespace Perf.Monads.Generator;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
sealed class ResultMonadGenerator : IIncrementalGenerator {
    const string InterfaceMarkerFullName = "Perf.Monads.Result.IResultMonad";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, ct) => {
                if (node is StructDeclarationSyntax {
                        BaseList.Types.Count: > 0
                    } s
                ) {
                    if (s.Modifiers.Any(SyntaxKind.PartialKeyword) is false
                     || s.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) is false) {
                        return false;
                    }

                    foreach (var bt in s.BaseList.Types) {
                        if (bt is {
                            Type: GenericNameSyntax {
                                Identifier.Text : "IResultMonad",
                                TypeArgumentList.Arguments.Count: 2
                            }
                        }) {
                            return true;
                        }
                    }

                    return false;
                }

                return false;
            },
            static (context, ct) => {
                var syntax = (StructDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
                    return default;
                }

                var hasOneInterface = false;
                foreach (var i in symbol.Interfaces) {
                    if (i.FullPath() is InterfaceMarkerFullName) {
                        if (hasOneInterface) {
                            return default;
                        }

                        hasOneInterface = true;
                    }
                }

                return hasOneInterface ? (symbol, syntax) : default;
            }
        );
        var filtered = types.Where(static x => x != default);

        context.RegisterSourceOutput(
            filtered,
            static (context, tuple) => {
                var (symbol, _) = tuple;
                var i = symbol!.Interfaces.First(x => x.FullPath() is InterfaceMarkerFullName);
                var arg1 = i.TypeArguments[0];
                var arg2 = i.TypeArguments[1];

                var sourceText = string.Format(
                    Pattern,
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
                    },
                    symbol.TypeArguments.Length switch {
                        1 when symbol.TypeArguments[0] is ITypeParameterSymbol t1 => $"<{t1.Name}> where {t1.Name} : notnull",
                        1 when symbol.TypeArguments[0] is { } t1                  => $"<{t1.MinimalName()}>",
                        2 => (symbol.TypeArguments[0], symbol.TypeArguments[1]) switch {
                            (ITypeParameterSymbol t1, ITypeParameterSymbol t2) =>
                                $"<{t1.Name}, {t2.Name}> where {t1.Name} : notnull where {t2.Name} : notnull",
                            (ITypeParameterSymbol t1, var t2) => $"<{t1.Name}, {t2.MinimalName()}> where {t1.Name} : notnull",
                            (var t1, ITypeParameterSymbol t2) => $"<{t1.MinimalName()}, {t2.Name}> where {t2.Name} : notnull",
                            var (t1, t2)                      => $"<{t1.MinimalName()}, {t2.MinimalName()}>"
                        },
                        _ => null
                    },
                    symbol.TypeArguments.Length switch {
                        1 => "<>",
                        2 => "<,>",
                        _ => null
                    }
                );

                context.AddSource($"{MinimalNameWithGenericMetadata(symbol)}.g.cs", SourceText.From(sourceText, Encoding.UTF8));

                static string MinimalNameWithGenericMetadata(INamedTypeSymbol symbol) {
                    return symbol.IsGenericType ? $"{symbol.Name}`{symbol.TypeParameters.Length}" : symbol.Name;
                }
            }
        );
    }

    const string Pattern = """
        // <auto-generated />
        #nullable enable

        namespace {4};

        file sealed class {0}_DebugView{6} {{
            public {0}_DebugView({3} result) {{
                this.State = result.State;
                this.Value = this.State > 0 ? result.IsOk ? result.Ok : result.Error : "Uninitialized";
            }}
        
            public global::Perf.Monads.Result.ResultState State {{ get; }}
            public object? Value {{ get; }}
        }}

        [global::System.Diagnostics.DebuggerTypeProxy(typeof({0}_DebugView{7}))]
        [global::System.Diagnostics.DebuggerDisplay("{{state > 0 ? (IsOk ? \"Ok: \" + ok.ToString() : $\"Error: \" + error.ToString()) : \"Uninitialized\"}}")]
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        readonly partial struct {3} : global::System.IEquatable<{3}>, global::System.IEquatable<global::Perf.Monads.Result.Result<{1}, {2}>> {{
            public {0}() {{
                state = global::Perf.Monads.Result.ResultState.Uninitialized;
                ok = default!;
                error = default!;
            }}
            public {0}({1} ok) {{
                state = global::Perf.Monads.Result.ResultState.Ok;
                this.ok = ok;
                error = default!;
            }}
            public {0}({2} error) {{
                state = global::Perf.Monads.Result.ResultState.Error;
                ok = default!;
                this.error = error;
            }}
            public {0}(global::Perf.Monads.Result.Result.Ok<{1}> ok) : this(ok: ok.Value) {{ }}
            public {0}(global::Perf.Monads.Result.Result.Error<{2}> error) : this(error: error.Value) {{ }}
            
            private readonly global::Perf.Monads.Result.ResultState state;
            private readonly {1} ok;
            private readonly {2} error;
            
            private static readonly string UninitializedException = $"{5} is Unitialized";
            private static readonly string ErrorAccessException = $"Cannot access Error. {5} is Ok";
            private static readonly string OkAccessException = $"Cannot access Ok. {5} is Error";
            
            [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {1} Ok =>
                state switch {{
                    global::Perf.Monads.Result.ResultState.Uninitialized => throw new global::System.InvalidOperationException(UninitializedException),
                    global::Perf.Monads.Result.ResultState.Ok            => ok,
                    global::Perf.Monads.Result.ResultState.Error         => throw new global::System.InvalidOperationException(ErrorAccessException),
                    _                                                    => throw new global::System.ArgumentOutOfRangeException()
                }};
            [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {2} Error =>
                state switch {{
                    global::Perf.Monads.Result.ResultState.Uninitialized => throw new global::System.InvalidOperationException(UninitializedException),
                    global::Perf.Monads.Result.ResultState.Ok            => throw new global::System.InvalidOperationException(OkAccessException),
                    global::Perf.Monads.Result.ResultState.Error         => error,
                    _                                                    => throw new global::System.ArgumentOutOfRangeException()
                }};
            public bool IsOk => state is global::Perf.Monads.Result.ResultState.Ok;
            public global::Perf.Monads.Result.ResultState State => state;
        // Operators
            public static implicit operator {3}({1} ok) => new(ok: ok);
            public static implicit operator {3}(global::Perf.Monads.Result.Result.Ok<{1}> ok) => new(ok: ok.Value);
            public static implicit operator {3}({2} error) => new(error: error);
            public static implicit operator {3}(global::Perf.Monads.Result.Result.Error<{2}> error) => new(error: error.Value);
            public static implicit operator global::Perf.Monads.Result.Result<{1}, {2}>({3} m) => m.IsOk ? new(ok: m.ok) : new(error: m.error);
            public static implicit operator {3}(global::Perf.Monads.Result.Result<{1}, {2}> r) => r.IsOk ? new(ok: r.Ok) : new(error: r.Error);
            public static implicit operator bool({3} monad) => monad.IsOk;

        // Equality
            public bool Equals({3} other) {{
                if (state != other.state) {{
                    return false;
                }}
            
                if (state is global::Perf.Monads.Result.ResultState.Ok) {{
                    return EqualityComparer<{1}>.Default.Equals(ok, other.ok);
                }}
            
                if (state is global::Perf.Monads.Result.ResultState.Error) {{
                    return EqualityComparer<{2}>.Default.Equals(error, other.error);
                }}
            
                throw new InvalidOperationException("Cannot compare different states");
            }}
            public bool Equals(global::Perf.Monads.Result.Result<{1}, {2}> other) => other.Equals((global::Perf.Monads.Result.Result<{1}, {2}>)this);
            public override bool Equals(object? obj) => obj is {3} other && Equals(other);
            public override int GetHashCode() =>
                state switch {{
                    global::Perf.Monads.Result.ResultState.Ok    => ok.GetHashCode(),
                    global::Perf.Monads.Result.ResultState.Error => error.GetHashCode(),
                    _                                            => 0
                }};

        // Map
            public global::Perf.Monads.Result.Result<TNewOk, {2}> Map<TNewOk>(Func<{1}, TNewOk> mapOk) where TNewOk : notnull => IsOk ? mapOk(ok) : error;
            public async ValueTask<global::Perf.Monads.Result.Result<TNewOk, {2}>> Map<TNewOk>(Func<{1}, global::System.Threading.Tasks.ValueTask<TNewOk>> mapOk) where TNewOk : notnull => IsOk ? await mapOk(ok) : error;
        
            public global::Perf.Monads.Result.Result<{1}, TNewError> MapError<TNewError>(Func<{2}, TNewError> mapError) where TNewError : notnull => IsOk ? ok : mapError(error);
            public async ValueTask<global::Perf.Monads.Result.Result<{1}, TNewError>> MapError<TNewError>(Func<{2}, global::System.Threading.Tasks.ValueTask<TNewError>> mapError) where TNewError : notnull => IsOk ? ok : await mapError(error);
        
            public global::Perf.Monads.Result.Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
                Func<{1}, TNewOk> mapOk,
                Func<{2}, TNewError> mapError
            ) where TNewOk : notnull where TNewError : notnull => IsOk ? mapOk(ok) : mapError(error);
            public async ValueTask<global::Perf.Monads.Result.Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
                Func<{1}, ValueTask<TNewOk>> mapOk,
                Func<{2}, ValueTask<TNewError>> mapError
            ) where TNewOk : notnull where TNewError : notnull => IsOk ? await mapOk(ok) : await mapError(error);
        }}
        """;
}
