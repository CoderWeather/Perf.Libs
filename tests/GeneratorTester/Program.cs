using GeneratorTester;
using PerfXml.Generator;

_ = PerfXml.Xml.DefaultResolver;

var filePath = @"/Users/coderweather/Documents/src/private-work/Perf.Libs/tests/GeneratorTester/_Test_Xml_Generation.cs";
GeneratorTesting.Test<XmlGenerator>(filePath);

Console.WriteLine("END");
