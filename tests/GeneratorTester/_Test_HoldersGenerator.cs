// namespace GeneratorTester;

using Perf.Holders;

partial class C1Class {
    partial struct C2Struct {
        partial record C3Record {
            partial record struct C4RecordStruct {
                static partial class C5StaticClass {
                    partial struct InnerInnerOption<T> : IOptionHolder<T>;
                }
            }
        }
    }
}