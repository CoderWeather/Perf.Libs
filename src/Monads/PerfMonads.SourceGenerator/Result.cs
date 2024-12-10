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

        using System.Threading.Tasks;
        using Perf.Monads.Result;

        file sealed class {0}_DebugView{6} {{
            public {0}_DebugView({3} result) {{
                this.result = result;
                this.Init = (bool?)InitField.GetValue(this.result) ?? false;
            }}
        
            readonly Result<{1}, {2}> result;
            static readonly global::System.Reflection.FieldInfo InitField = typeof(Result<{1}, {2}>)
                .GetField("init", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic)!;
        
            public bool Init {{ get; }}
            public bool IsOk => Init && result.IsOk;
            public bool IsError => Init && result.IsError;
            public {1} Ok => Init && result.IsOk ? result.Ok : default!;
            public {2} Error => Init && result.IsError ? result.Error : default!;
        }}

        [global::System.Diagnostics.DebuggerTypeProxy(typeof({0}_DebugView{7}))]
        [global::System.Diagnostics.DebuggerDisplay("{{init ? (isOk ? \"Ok: \" + ok.ToString() : $\"Error: \" + error.ToString()) : \"Empty\"}}")]
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
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
            [global::System.Diagnostics.DebuggerBrowsableAttribute(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {1} Ok => init ? isOk ? ok : throw new InvalidOperationException($"Cannot access Ok. {5} is Error") : throw new InvalidOperationException($"{5} is Empty");
            [global::System.Diagnostics.DebuggerBrowsableAttribute(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public {2} Error => init ? isOk is false ? error : throw new InvalidOperationException($"Cannot access Error. {5} is Ok") : throw new InvalidOperationException($"{5} is Empty");
            [global::System.Diagnostics.DebuggerBrowsableAttribute(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool IsOk => init ? isOk : throw new InvalidOperationException($"{5} is Empty");
            [global::System.Diagnostics.DebuggerBrowsableAttribute(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool IsError => init ? isOk is false : throw new InvalidOperationException($"{5} is Empty");
        // Operators
            public static implicit operator {3}({1} ok) => new(ok: ok);
            public static implicit operator {3}(Result.Ok<{1}> ok) => new(ok: ok.Value);
            public static implicit operator {3}({2} error) => new(error: error);
            public static implicit operator {3}(Result.Error<{2}> error) => new(error: error.Value);
            public static implicit operator Result<{1}, {2}>({3} m) => m.IsOk ? new(ok: m.ok) : new(error: m.error);
            public static implicit operator {3}(Result<{1}, {2}> r) => r.IsOk ? new(ok: r.Ok) : new(error: r.Error);
            public static implicit operator bool({3} monad) => monad.IsOk;

        // Equality
            public bool Equals({3} other) =>
                IsOk && other.IsOk && EqualityComparer<{1}>.Default.Equals(ok, other.ok)
             || IsOk is false && other.IsOk is false && EqualityComparer<{2}>.Default.Equals(error, other.error);
            public bool Equals(Result<{1}, {2}> other) => other.Equals((Result<{1}, {2}>)this);
            public override bool Equals(object? obj) => obj is {3} other && Equals(other);
            public override int GetHashCode() => IsOk ? ok.GetHashCode() : error.GetHashCode();

        // Map
            public Result<TNewOk, {2}> Map<TNewOk>(Func<{1}, TNewOk> mapOk) where TNewOk : notnull => IsOk ? mapOk(ok) : error;
            public async ValueTask<Result<TNewOk, {2}>> Map<TNewOk>(Func<{1}, ValueTask<TNewOk>> mapOk) where TNewOk : notnull => IsOk ? await mapOk(ok) : error;
        
            public Result<{1}, TNewError> MapError<TNewError>(Func<{2}, TNewError> mapError) where TNewError : notnull => IsOk ? ok : mapError(error);
            public async ValueTask<Result<{1}, TNewError>> MapError<TNewError>(Func<{2}, ValueTask<TNewError>> mapError) where TNewError : notnull => IsOk ? ok : await mapError(error);
        
            public Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
                Func<{1}, TNewOk> mapOk,
                Func<{2}, TNewError> mapError
            ) where TNewOk : notnull where TNewError : notnull => IsOk ? mapOk(ok) : mapError(error);
            public async ValueTask<Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
                Func<{1}, ValueTask<TNewOk>> mapOk,
                Func<{2}, ValueTask<TNewError>> mapError
            ) where TNewOk : notnull where TNewError : notnull => IsOk ? await mapOk(ok) : await mapError(error);
            
            public Result() {
            state = 0;
            ok = default!;
            error = default!;
        }
        
        public Result(TOk ok) {
            state = ResultState.Ok;
            this.ok = ok;
            error = default!;
        }
        
        public Result(TError error) {
            state = ResultState.Error;
            ok = default!;
            this.error = error;
        }
        
        public Result(Result.Ok<TOk> ok) : this(ok: ok.Value) { }
        public Result(Result.Error<TError> error) : this(error: error.Value) { }
        private readonly ResultState state;
        private readonly TOk ok;
        private readonly TError error;
        private static readonly string UninitializedException = $"Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Unitialized";
        private static readonly string ErrorAccessException = $"Cannot access Error. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Ok";
        private static readonly string OkAccessException = $"Cannot access Ok. Result<{typeof(TOk).Name}, {typeof(TError).Name}> is Error";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TOk Ok =>
            state switch {
                ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                ResultState.Ok            => ok,
                ResultState.Error         => throw new InvalidOperationException(ErrorAccessException),
                _                         => throw new ArgumentOutOfRangeException()
            };
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TError Error =>
            state switch {
                ResultState.Uninitialized => throw new InvalidOperationException(UninitializedException),
                ResultState.Ok            => throw new InvalidOperationException(OkAccessException),
                ResultState.Error         => error,
                _                         => throw new ArgumentOutOfRangeException()
            };
        public bool IsOk => state is ResultState.Ok;
        public ResultState State => state;
        public static implicit operator Result<TOk, TError>(TOk ok) => new(ok);
        public static implicit operator Result<TOk, TError>(Result.Ok<TOk> ok) => new(ok.Value);
        public static implicit operator Result<TOk, TError>(TError error) => new(error);
        public static implicit operator Result<TOk, TError>(Result.Error<TError> error) => new(error.Value);
        
        public bool Equals(Result<TOk, TError> other) {
            if (state != other.state) {
                return false;
            }
        
            if (state is ResultState.Ok) {
                return EqualityComparer<TOk>.Default.Equals(ok, other.ok);
            }
        
            if (state is ResultState.Error) {
                return EqualityComparer<TError>.Default.Equals(error, other.error);
            }
        
            throw new InvalidOperationException();
        }
        
        public override bool Equals(object? obj) => obj is Result<TOk, TError> other && Equals(other);
        
        public override int GetHashCode() =>
            state switch {
                ResultState.Ok    => ok.GetHashCode(),
                ResultState.Error => error.GetHashCode(),
                _                 => 0
            };
        
        // Map
        public Result<TNewOk, TNewError> Map<TNewOk, TNewError>(
            Func<TOk, TNewOk> mapOk,
            Func<TError, TNewError> mapError
        ) where TNewOk : notnull where TNewError : notnull {
            return IsOk ? mapOk(ok) : mapError(error);
        }
        
        public async ValueTask<Result<TNewOk, TNewError>> Map<TNewOk, TNewError>(
            Func<TOk, ValueTask<TNewOk>> mapOk,
            Func<TError, ValueTask<TNewError>> mapError
        ) where TNewOk : notnull where TNewError : notnull {
            return IsOk ? await mapOk(ok) : await mapError(error);
        }
        
        public Result<TNewOk, TError> Map<TNewOk>(Func<TOk, TNewOk> mapOk) where TNewOk : notnull => IsOk ? mapOk(ok) : error;
        
        public async ValueTask<Result<TNewOk, TError>> Map<TNewOk>(Func<TOk, ValueTask<TNewOk>> mapOk) where TNewOk : notnull =>
            IsOk ? await mapOk(ok) : error;
        
        public Result<TOk, TNewError> MapError<TNewError>(Func<TError, TNewError> mapError) where TNewError : notnull => IsOk ? ok : mapError(error);
        
        public async ValueTask<Result<TOk, TNewError>> MapError<TNewError>(Func<TError, ValueTask<TNewError>> mapError) where TNewError : notnull =>
            IsOk ? ok : await mapError(error);
        }}
        """;
}
