namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

readonly record struct OptionHolderContextInfo(
    string SourceFileName,
    string? Namespace,
    OptionHolderContextInfo.OptionInfo Option,
    OptionHolderContextInfo.SomeInfo Some,
    OptionHolderContextInfo.IsSomeInfo IsSome,
    EquatableList<HolderContainingType> ContainingTypes = default,
    OptionHolderContextInfo.OptionConfiguration Configuration = default
) {
    public readonly record struct OptionConfiguration(
        bool? ImplicitCastSomeTypeToOption,
        bool? IncludeOptionSomeObject
    ) {
        public OptionConfiguration MergeWithMajor(OptionConfiguration other) {
            return new() {
                ImplicitCastSomeTypeToOption = other.ImplicitCastSomeTypeToOption ?? ImplicitCastSomeTypeToOption ?? null,
                IncludeOptionSomeObject = other.IncludeOptionSomeObject ?? IncludeOptionSomeObject ?? null
            };
        }

        public OptionConfiguration ApplyDefaults() {
            return new() {
                ImplicitCastSomeTypeToOption = ImplicitCastSomeTypeToOption ?? true,
                IncludeOptionSomeObject = IncludeOptionSomeObject ?? true
            };
        }
    }

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

static class OptionConfigurationExt {
    public static OptionHolderContextInfo.OptionConfiguration ReadOptionConfigurationFromAttributes(
        this ImmutableArray<AttributeData> attributes
    ) {
        var attributeData = attributes
            .FirstOrDefault(x => x.AttributeClass?.FullPath().Equals(HolderTypeNames.OptionConfigurationFullName) ?? false);
        if (attributeData is null) {
            return default;
        }

        var configuration = new OptionHolderContextInfo.OptionConfiguration();
        foreach (var na in attributeData.NamedArguments) {
            switch (na.Key) {
                case "ImplicitCastSomeTypeToOption":
                    configuration = configuration with {
                        ImplicitCastSomeTypeToOption = na.Value.Value is true
                    };
                    break;
                case "IncludeOptionSomeObject":
                    configuration = configuration with {
                        IncludeOptionSomeObject = na.Value.Value is true
                    };
                    break;
                default: continue;
            }
        }

        return configuration;
    }

    public static IncrementalValueProvider<OptionHolderContextInfo.OptionConfiguration> ReadOptionConfiguration(
        this IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider
    ) =>
        optionsProvider.Select(static (x, _) => {
                var options = x.GlobalOptions;
                OptionHolderContextInfo.OptionConfiguration configuration = default;
                if (options.TryGetValue("PerfHoldersOptionImplicitCastSomeTypeToOption", out var s1) && s1 is not null and not "") {
                    configuration = configuration with {
                        ImplicitCastSomeTypeToOption =
                        s1.Equals("enable", StringComparison.OrdinalIgnoreCase) || s1.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s1.Equals("disable", StringComparison.OrdinalIgnoreCase) || s1.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                if (options.TryGetValue("PerfHoldersOptionIncludeOptionSomeObject", out var s2) && s2 is not null and not "") {
                    configuration = configuration with {
                        IncludeOptionSomeObject =
                        s2.Equals("enable", StringComparison.OrdinalIgnoreCase) || s2.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s2.Equals("disable", StringComparison.OrdinalIgnoreCase) || s2.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                return configuration;
            }
        );
}
