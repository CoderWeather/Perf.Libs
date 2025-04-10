namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

readonly record struct OptionHolderContextInfo(
    string MetadataName,
    string? Namespace,
    OptionHolderContextInfo.OptionInfo Option,
    OptionHolderContextInfo.SomeInfo Some,
    OptionHolderContextInfo.IsSomeInfo IsSome,
    EquatableList<HolderContainingType> ContainingTypes = default,
    OptionHolderContextInfo.OptionConfiguration Configuration = default,
    CompInfo CompInfo = default
) {
    public readonly record struct OptionConfiguration(
        bool? ImplicitCastSomeTypeToOption,
        bool? IncludeOptionSomeObject,
        bool? PublicState,
        bool? AddCastByRefMethod,
        bool? GenerateSystemTextJsonConverter,
        bool? GenerateMessagePackFormatter
    ) {
        public OptionConfiguration MergeWithMajor(OptionConfiguration other) {
            return new(
                ImplicitCastSomeTypeToOption: other.ImplicitCastSomeTypeToOption ?? ImplicitCastSomeTypeToOption ?? null,
                IncludeOptionSomeObject: other.IncludeOptionSomeObject ?? IncludeOptionSomeObject ?? null,
                PublicState: other.PublicState ?? PublicState ?? null,
                AddCastByRefMethod: other.AddCastByRefMethod ?? AddCastByRefMethod ?? null,
                GenerateSystemTextJsonConverter: other.GenerateSystemTextJsonConverter ?? GenerateSystemTextJsonConverter ?? null,
                GenerateMessagePackFormatter: other.GenerateMessagePackFormatter ?? GenerateMessagePackFormatter ?? null
            );
        }

        public OptionConfiguration ApplyDefaults() {
            return new(
                ImplicitCastSomeTypeToOption: ImplicitCastSomeTypeToOption ?? true,
                IncludeOptionSomeObject: IncludeOptionSomeObject ?? true,
                PublicState: PublicState ?? false,
                AddCastByRefMethod: AddCastByRefMethod ?? false,
                GenerateSystemTextJsonConverter: GenerateSystemTextJsonConverter ?? false,
                GenerateMessagePackFormatter: GenerateMessagePackFormatter ?? false
            );
        }
    }

    public readonly record struct OptionInfo(
        string DeclarationName,
        string OnlyName,
        string GlobalName,
        TypeAccessibility Accessibility,
        int TypeParameterCount
    );

    public readonly record struct SomeInfo(
        bool IsStruct,
        bool IsTypeParameter,
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

    public bool ShouldGenerateJsonConverter() {
        if (CompInfo.SystemTextJsonAvailable is false) {
            return false;
        }

        if (Configuration.GenerateSystemTextJsonConverter is not true) {
            return false;
        }

        return GlobalAccessibility is TypeAccessibility.Public or TypeAccessibility.Internal;
    }

    public string GeneratedJsonConverterTypeForAttribute { get; } =
        $"global::Perf.Holders.Serialization.SystemTextJson.{(Some.IsTypeParameter ? "JsonConverterFactory" : "JsonConverter")}_{Option.OnlyName}";

    public bool ShouldGenerateMessagePackFormatter() {
        if (CompInfo.MessagePackAvailable is false) {
            return false;
        }

        if (Configuration.GenerateMessagePackFormatter is not true) {
            return false;
        }

        return GlobalAccessibility is TypeAccessibility.Public or TypeAccessibility.Internal;
    }

    public string GeneratedMessagePackFormatterTypeForAttribute() {
        return Some.IsTypeParameter
            ? $"global::Perf.Holders.Serialization.MessagePack.MessagePackFormatter_{Option.OnlyName}<>"
            : $"global::Perf.Holders.Serialization.MessagePack.MessagePackFormatter_{Option.OnlyName}";
    }

    public TypeAccessibility GlobalAccessibility { get; } =
        (TypeAccessibility)Math.Max((int)Option.Accessibility, (int)ContainingTypes.Max(x => x.Accessibility));
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
                case "PublicState":
                    configuration = configuration with {
                        PublicState = na.Value.Value is true
                    };
                    break;
                case "AddCastByRefMethod":
                    configuration = configuration with {
                        AddCastByRefMethod = na.Value.Value is true
                    };
                    break;
                case "GenerateSystemTextJsonConverter":
                    configuration = configuration with {
                        GenerateSystemTextJsonConverter = na.Value.Value is true
                    };
                    break;
                case "GenerateMessagePackFormatter":
                    configuration = configuration with {
                        GenerateMessagePackFormatter = na.Value.Value is true
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

                if (options.TryGetBool("PerfHoldersOptionImplicitCastSomeTypeToOption", out var b1)) {
                    configuration = configuration with {
                        ImplicitCastSomeTypeToOption = b1
                    };
                }

                if (options.TryGetBool("PerfHoldersOptionIncludeOptionSomeObject", out var b2)) {
                    configuration = configuration with {
                        IncludeOptionSomeObject = b2
                    };
                }

                if (options.TryGetBool("PerfHoldersOptionPublicState", out var b3)) {
                    configuration = configuration with {
                        PublicState = b3
                    };
                }

                if (options.TryGetBool("PerfHoldersOptionAddCastByRefMethod", out var b4)) {
                    configuration = configuration with {
                        AddCastByRefMethod = b4
                    };
                }

                if (options.TryGetBool("PerfHoldersOptionGenerateJsonConverter", out var b5)) {
                    configuration = configuration with {
                        GenerateSystemTextJsonConverter = b5
                    };
                }

                if (options.TryGetBool("PerfHoldersOptionGenerateMessagePackFormatter", out var b6)) {
                    configuration = configuration with {
                        GenerateMessagePackFormatter = b6
                    };
                }

                return configuration;
            }
        );
}
