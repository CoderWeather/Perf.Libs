

// GeneratorTesting.Test<ResultHolderGenerator>("/Users/coderweather/src/own_projects/Perf.Libs/tests/GeneratorTester/_Test_HoldersGenerator.cs");

var s = "Name";
var p = "dfgdfgdggfd{NameQualified}".AsSpan();

var entry = p.IndexOf('{');
var t = p[(entry + 1)..];
if (t.StartsWith(s.AsSpan())) {
    _ = t;
}

Console.WriteLine("END");

