namespace GeneratorTester;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class GeneratorTesting {
    public static void Test<T>(params string[] sourceCodeFilePath) where T : class, IIncrementalGenerator, new() {
        var syntax = sourceCodeFilePath.Select(
            path => {
                var text = File.ReadAllText(path);
                var s = CSharpSyntaxTree.ParseText(text, path: path);
                return s;
            }
        );

        var references = AppDomain.CurrentDomain.GetAssemblies()
           .Select(x => x.GetName())
           .Concat(Assembly.GetExecutingAssembly().GetReferencedAssemblies())
           .Append(Assembly.GetExecutingAssembly().GetName())
           // .DistinctBy(x => x.FullName)
           .Select(Assembly.Load)
           .Select(a => MetadataReference.CreateFromFile(a.Location))
           .Cast<MetadataReference>()
           .ToArray();

        var compilation = CSharpCompilation.Create(
            "Source.Generator.Tests",
            syntax,
            references,
            new(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable,
                metadataImportOptions: MetadataImportOptions.All,
                warningLevel: 4,
                checkOverflow: true,
                concurrentBuild: true,
                deterministic: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: null
            )
        );

        // foreach (var d in compilation.GetDeclarationDiagnostics()) {
        //     Console.WriteLine($"[{d.Descriptor.Id}] {d.GetMessage()}");
        // }

        var generator = new T();

        CSharpGeneratorDriver.Create(generator)
           .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics
            );

        _ = outputCompilation;
        foreach (var d in diagnostics) {
            Console.WriteLine($"[{d.Descriptor.Id}] {d.GetMessage()}");
        }
    }
}
