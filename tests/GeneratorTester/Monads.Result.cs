namespace GeneratorTester;

using Perf.Monads;
using Perf.Monads.Result;

readonly partial struct EmptyResult : IResultMonad<Unit, string>;
