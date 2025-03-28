namespace GeneratorTester;

using Perf.Holders;

partial class C1Class {
    partial struct C2Struct {
        partial record C3Record {
            partial record struct C4RecordStruct {
                static partial class C5StaticClass {
                    partial struct InnerInnerOption<T> : IOptionHolder<T> where T : notnull;
                }
            }
        }
    }
}

partial struct BasicMaybe : IOptionHolder<int> {
    public partial int One { get; }
    public partial bool Two { get; }
}

partial struct BasicRefMaybe : IOptionHolder<string>;

partial struct ComplexMaybe : IOptionHolder<string> {
    public partial string Complex { get; }
    public partial bool Done { get; }
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
