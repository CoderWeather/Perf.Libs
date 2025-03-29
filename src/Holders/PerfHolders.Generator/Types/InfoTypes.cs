// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

readonly record struct CompInfo(
    LanguageVersion Version,
    OptimizationLevel OptimizationLevel
) {
    public bool SupportFileVisibilityModifier() => Version >= LanguageVersion.CSharp11;
    public bool SupportNullableAnnotation() => Version >= LanguageVersion.CSharp8;
    public bool SupportFileScopedNamespace() => Version >= LanguageVersion.CSharp10;
}

static class CompInfoExtensions {
    public static IncrementalValueProvider<CompInfo> SelectCompInfo(this IncrementalValueProvider<Compilation> compilationProvider) =>
        compilationProvider.Select(
            static (c, _) => c is CSharpCompilation comp
                ? new CompInfo(
                    Version: comp.LanguageVersion,
                    OptimizationLevel: comp.Options.OptimizationLevel
                )
                : default
        );
}

readonly record struct HolderContainingType(string Kind, string Name);

readonly record struct ResultHolderContextInfo(
    string SourceFileName,
    string? Namespace,
    ResultHolderContextInfo.ResultInfo Result,
    ResultHolderContextInfo.OkInfo Ok,
    ResultHolderContextInfo.ErrorInfo Error,
    ResultHolderContextInfo.IsOkInfo IsOk,
    EquatableList<HolderContainingType> ContainingTypes
) {
    public readonly record struct Feature(string Name, bool Enabled);

    public readonly record struct ResultInfo(
        string DeclarationName,
        string OnlyName,
        string GlobalName,
        int TypeArgumentCount
    );

    public readonly record struct OkInfo(
        bool IsStruct,
        bool IsTypeArgument,
        string Property,
        string Field,
        string Type,
        string TypeNullable,
        bool HavePartial
    ) {
        public const string DefaultProperty = "Ok";
        public const string DefaultField = "ok";
    }

    public readonly record struct ErrorInfo(
        bool IsStruct,
        bool IsTypeArgument,
        string Property,
        string Field,
        string Type,
        string TypeNullable,
        bool HavePartial
    ) {
        public const string DefaultProperty = "Error";
        public const string DefaultField = "error";
    }

    public readonly record struct IsOkInfo(
        string Property,
        bool HavePartial
    ) {
        public const string DefaultProperty = "IsOk";
    }
}

readonly record struct OptionHolderContextInfo(
    string SourceFileName,
    string? Namespace,
    OptionHolderContextInfo.OptionInfo Option,
    OptionHolderContextInfo.SomeInfo Some,
    OptionHolderContextInfo.IsSomeInfo IsSome,
    EquatableList<HolderContainingType> ContainingTypes = default,
    EquatableList<OptionHolderContextInfo.Feature> Features = default
) {
    public readonly record struct Feature(string Name, bool Enabled);

    public readonly record struct OptionInfo(
        string DeclarationName,
        string OnlyName,
        string GlobalName,
        int TypeArgumentCount
    );

    public readonly record struct SomeInfo(
        bool IsStruct,
        bool IsTypeArgument,
        string Property,
        string Field,
        string Type,
        string TypeNullable,
        bool HavePartial
    ) {
        public const string DefaultProperty = "Some";
        public const string DefaultField = "some";
    }

    public readonly record struct IsSomeInfo(
        string Property,
        bool HavePartial
    ) {
        public const string DefaultProperty = "IsSome";
    }
}
