// _ = PerfXml.Xml.DefaultResolver;

// var filePath = @"/Users/coderweather/Documents/src/private-work/Perf.Libs/tests/GeneratorTester/_Test_Xml_Generation.cs";
// GeneratorTesting.Test<XmlGenerator>(filePath);

using System.Diagnostics;
using GeneratorTester;
using Perf.Monads;
using Perf.Monads.Result;

EmptyResult r1 = default(Unit);
_ = r1;
EmptyResult r2 = "error";
_ = r2;
EmptyResult r3 = default;
_ = r3;


Console.WriteLine("END");
