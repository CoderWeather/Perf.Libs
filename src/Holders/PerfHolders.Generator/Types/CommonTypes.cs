// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

enum TypeAccessibility {
    None = 0,
    Public = 1,
    Internal = 2,
    Private = 3
}

enum HolderType {
    None = 0,
    Result = 1,
    Option = 2,
    MultiResult = 3
}

readonly record struct CompInfo(
    LanguageVersion Version,
    bool SystemTextJsonAvailable = false,
    bool GenericSerializerSystemTextJsonAvailable = false,
    bool MessagePackAvailable = false,
    bool GenericSerializerMessagePackAvailable = false
) {
    public bool SupportFileVisibilityModifier() => Version >= LanguageVersion.CSharp11;
    public bool SupportNullableAnnotation() => Version >= LanguageVersion.CSharp8;
    public bool SupportFileScopedNamespace() => Version >= LanguageVersion.CSharp10;
}

static class CompInfoExtensions {
    public static IncrementalValueProvider<CompInfo> SelectCompInfo(
        this IncrementalValueProvider<Compilation> compilationProvider,
        HolderType holderType
    ) =>
        compilationProvider.Select((c, _) => c is CSharpCompilation comp
            ? new CompInfo(
                Version: comp.LanguageVersion,
                SystemTextJsonAvailable: comp.GetTypeByMetadataName("System.Text.Json.Serialization.JsonConverterAttribute") is not null,
                GenericSerializerSystemTextJsonAvailable: holderType switch {
                    HolderType.Result      => comp.GetTypeByMetadataName(HolderTypeNames.ResultSerializationSystemTextJson) is not null,
                    HolderType.Option      => comp.GetTypeByMetadataName(HolderTypeNames.OptionSerializationSystemTextJson) is not null,
                    HolderType.MultiResult => comp.GetTypeByMetadataName(HolderTypeNames.MultiResultSerializationSystemTextJson) is not null,
                    _                      => false
                },
                MessagePackAvailable: comp.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute") is not null,
                GenericSerializerMessagePackAvailable: holderType switch {
                    HolderType.Result      => comp.GetTypeByMetadataName(HolderTypeNames.ResultSerializationMessagePack) is not null,
                    HolderType.Option      => comp.GetTypeByMetadataName(HolderTypeNames.OptionSerializationMessagePack) is not null,
                    HolderType.MultiResult => comp.GetTypeByMetadataName(HolderTypeNames.MultiResultSerializationMessagePack) is not null,
                    _                      => false
                }
            )
            : default
        );
}

readonly record struct HolderContainingType(TypeAccessibility Accessibility, string Kind, string Name);

/// <summary>
/// For older than net9 compatibility
/// </summary>
readonly record struct HolderPropertyNameOverride(ITypeSymbol Type, string Name, string? IsName);

static class HolderPropertyNameOverrideExtensions {
    public static HolderPropertyNameOverride[] ReadPropertyNamesOverrides(
        this ImmutableArray<AttributeData> attributes
    ) {
        var attributeData = attributes
            .Where(x => x.AttributeClass?.FullPath().Equals(HolderTypeNames.HolderPropertyNameOverrideAttributeFullName) ?? false)
            .ToArray();
        if (attributeData is [ ]) {
            return [ ];
        }

        var results = new HolderPropertyNameOverride[attributeData.Length];

        for (var i = 0; i < attributeData.Length; i++) {
            var attribute = attributeData[i];

            results[i] = new(
                Type: (ITypeSymbol)attribute.ConstructorArguments[0].Value!,
                Name: (string)attribute.ConstructorArguments[1].Value!,
                IsName: attribute.ConstructorArguments[2].Value as string
            );
        }

        return results;
    }
}
