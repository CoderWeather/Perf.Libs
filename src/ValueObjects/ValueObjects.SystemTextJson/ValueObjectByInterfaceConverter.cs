using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Perf.ValueObjects;

public sealed class ValueObjectJsonConverterFactory : JsonConverterFactory {
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
    static readonly Type ValueObjectInterface = typeof(IValueObject<>);

#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)]
#endif
    static readonly Type NullableType = typeof(Nullable<>);

    public override bool CanConvert(Type typeToConvert) {
        if (typeToConvert.IsConstructedGenericType && typeToConvert.GetGenericTypeDefinition() == NullableType) {
            typeToConvert = typeToConvert.GenericTypeArguments[0];
        }

        return typeToConvert.IsValueType
         && typeToConvert.GetInterfaces()
               .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == ValueObjectInterface);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var isNullable = false;
        if (typeToConvert.IsConstructedGenericType && typeToConvert.GetGenericTypeDefinition() == NullableType) {
            typeToConvert = typeToConvert.GenericTypeArguments[0];
            isNullable = true;
        }

        var i = typeToConvert.GetInterfaces()
           .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == ValueObjectInterface);
        var innerType = i.GenericTypeArguments[0];

        var converterType = isNullable
            ? typeof(ValueObjectByInterfaceConverter<,>)
            : typeof(NullableValueObjectByInterfaceConverter<,>);

        var constructedConverterType = converterType.MakeGenericType(typeToConvert, innerType);
        var fieldInfo = constructedConverterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;

        var converterValue = fieldInfo.GetValue(null);

        return converterValue as JsonConverter;
    }
}

sealed class ValueObjectByInterfaceConverter<TValueObject, TValueType> : JsonConverter<TValueObject>
    where TValueObject : struct, IValueObject<TValueType> {
    ValueObjectByInterfaceConverter() { }
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
    internal static readonly Type Type = typeof(ValueObjectByInterfaceConverter<,>);

    public static readonly JsonConverter Instance = new ValueObjectByInterfaceConverter<TValueObject, TValueType>();

    public override TValueObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // check typeToConvert is number
        var typeIsNumber = typeToConvert is { IsPrimitive: true, IsValueType: true };
        var span = reader.GetSpan();
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, TValueObject value, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
}

sealed class NullableValueObjectByInterfaceConverter<TValueObject, TValueType> : JsonConverter<TValueObject?>
    where TValueObject : struct, IValueObject<TValueType> {
    NullableValueObjectByInterfaceConverter() { }
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
    internal static readonly Type Type = typeof(NullableValueObjectByInterfaceConverter<,>);

    public static readonly JsonConverter Instance = new NullableValueObjectByInterfaceConverter<TValueObject, TValueType>();

    public override TValueObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is JsonTokenType.Null) {
            return default;
        }

        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, TValueObject? value, JsonSerializerOptions options) {
        if (value.HasValue is false) {
            writer.WriteNullValue();
        }
    }
}

static partial class JsonHelpers {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetSpan(this ref Utf8JsonReader reader) {
        return reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
    }
}
