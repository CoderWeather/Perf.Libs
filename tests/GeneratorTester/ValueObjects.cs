// ReSharper disable UnusedType.Global

// ReSharper disable PartialTypeWithSinglePart

namespace GeneratorTester;

using Perf.ValueObjects;

// partial record RecValueObject(string Key, string Value) : IValueObject<string>;
//
// partial class ClsValueObject : IValueObject<string> {
//     public string Value { get; } = "";
//     public string Key { get; } = "";
// }
// ref partial struct RefValueObject : IValueObject<string> { }
partial struct CustomValueObject : IValueObject<string>;
partial struct CustomValueObject2 : IValidatableValueObject<string> {
    public bool Validate(string valueToValidate) {
        return true;
    }
}

// public readonly partial record struct CustomValueObject3 : IValueObject<string>;
// public readonly partial record struct CustomValueObject4 : IValueObject<string>;
// public readonly partial record struct CustomValueObject5 : IValueObject<string>;
// public readonly partial record struct CustomValueObject6 : IValueObject<string>;
// public readonly partial record struct CustomValueObject7 : IValueObject<string>;
// public readonly partial record struct CustomValueObject8 : IValueObject<string>;
// public readonly partial record struct CustomValueObject9 : IValueObject<string>;
// public readonly partial record struct CustomValueObject10 : IValueObject<string>;
// public readonly partial record struct CustomValueObject11 : IValueObject<string>;
// public readonly partial record struct CustomValueObject12 : IValueObject<string>;
// public readonly partial record struct CustomValueObject13 : IValueObject<string>;
// public readonly partial record struct CustomValueObject14 : IValueObject<string>;
// public readonly partial record struct CustomValueObject15 : IValueObject<string>;
// public readonly partial record struct CustomValueObject16 : IValueObject<string>;
// public readonly partial record struct CustomValueObject17 : IValueObject<string>;
