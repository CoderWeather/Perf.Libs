// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

readonly record struct CompInfo(
    LanguageVersion Version,
    OptimizationLevel OptimizationLevel,
    bool SerializationSystemTextJsonAvailable = false,
    bool SerializationMessagePackAvailable = false
) {
    public bool SupportFileVisibilityModifier() => Version >= LanguageVersion.CSharp11;
    public bool SupportNullableAnnotation() => Version >= LanguageVersion.CSharp8;
    public bool SupportFileScopedNamespace() => Version >= LanguageVersion.CSharp10;
}

static class CompInfoExtensions {
    public static IncrementalValueProvider<CompInfo> SelectCompInfo(this IncrementalValueProvider<Compilation> compilationProvider) =>
        compilationProvider.Select(static (c, _) => c is CSharpCompilation comp
            ? new CompInfo(
                Version: comp.LanguageVersion,
                OptimizationLevel: comp.Options.OptimizationLevel,
                SerializationSystemTextJsonAvailable:
                comp.GetTypeByMetadataName(HolderTypeNames.OptionSerializationSystemTextJson) is not null
                || comp.GetTypeByMetadataName(HolderTypeNames.ResultSerializationSystemTextJson) is not null,
                SerializationMessagePackAvailable:
                comp.GetTypeByMetadataName(HolderTypeNames.OptionSerializationMessagePack) is not null
                || comp.GetTypeByMetadataName(HolderTypeNames.ResultSerializationMessagePack) is not null
            )
            : default
        );
}

readonly record struct HolderContainingType(string Kind, string Name);

enum TypeAccessibility {
    None = 0,
    Public = 1,
    Internal = 2,
    Private = 3
}