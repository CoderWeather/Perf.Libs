// ReSharper disable RedundantUsingDirective

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GeneratorTester;
using Microsoft.CodeAnalysis.CSharp;
using Perf.Holders;
using Perf.Holders.Generator;

Console.WriteLine("END");

// BasicResult r = "error happened somewhere";
// ComplexResult cr = r.AsBase();
// _ = cr;
// BasicRefMaybe brm = "123";
// ComplexMaybe cm = brm.AsBase();
// _ = cm;

/*var r1 = await AsResultWithException();
_ = r1;
r1 = await AsResult();
_ = r1;

Console.WriteLine("END");*/

GeneratorTesting.Test<OptionHolderGenerator>("/Users/coderweather/src/own_projects/Perf.Libs/tests/GeneratorTester/_Test_HoldersGenerator.cs");

// static bool Check<T>(T? t)
//     where T : struct =>
//     t?.GetHashCode() > 0;
//
// static ValueTask<int> Clean() => new(10);
//
// static async ValueTask<int> ExceptionThrow() {
//     await Task.Delay(500);
//     throw new();
// }
//
// static async AwaitableResult<int> AsResult() {
//     var r1 = await Clean();
//     var r2 = await Clean();
//     return r1 + r2;
// }
//
// static async AwaitableResult<int> AsResultWithException() {
//     var r1 = await Clean();
//     var r2 = await ExceptionThrow();
//     return r1 + r2;
// }
