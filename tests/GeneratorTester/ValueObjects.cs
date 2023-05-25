namespace GeneratorTester;

using System.Diagnostics;
using Perf.ValueObjects;

[DebuggerDisplay("{Value}")]
public readonly partial record struct CustomValueObject : IValueObject<string>;

public static class ValueObject_Tests {
    public static void Test1() {
        CustomValueObject vo = new("test");
        _ = vo;
    }
}
