namespace GeneratorTester;

using Perf.Holders;

partial struct BasicMaybe : IOptionHolder<int>;
partial struct BasicRefMaybe : IOptionHolder<string>;

partial struct ComplexMaybe : IOptionHolder<string> {
    public partial string Complex { get; }
}

partial struct MaybeString : IOptionHolder<string> {
    public partial string Address { get; }
}

partial struct BasicResult : IResultHolder<int, string>;

partial struct ComplexResult : IResultHolder<int, string> {
    public partial int Count { get; }
    public partial string ExceptionText { get; }
    public partial bool Good { get; }
}

#pragma warning disable PRFH002
partial class NotCompilingResult : IResultHolder<int, char>, IOptionHolder<char>;
#pragma warning restore PRFH002

partial class NotCompilingResult {
    public int Ok { get; }
    public char Error { get; }
    public bool IsOk { get; }
    public char Some { get; }
    public bool IsSome { get; }
}
