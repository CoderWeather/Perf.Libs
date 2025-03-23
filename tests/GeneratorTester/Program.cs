// ReSharper disable RedundantUsingDirective

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GeneratorTester;
using Perf.Holders;

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
_ = 10;

static ValueTask<int> Clean() => new(10);

static async ValueTask<int> ExceptionThrow() {
    await Task.Delay(500);
    throw new();
}

static async AwaitableResult<int> AsResult() {
    var r1 = await Clean();
    var r2 = await Clean();
    return r1 + r2;
}

static async AwaitableResult<int> AsResultWithException() {
    var r1 = await Clean();
    var r2 = await ExceptionThrow();
    return r1 + r2;
}
