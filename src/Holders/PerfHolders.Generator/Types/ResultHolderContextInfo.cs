namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

readonly record struct ResultHolderContextInfo(
    string MetadataName,
    string? Namespace,
    ResultHolderContextInfo.ResultInfo Result,
    ResultHolderContextInfo.OkInfo Ok,
    ResultHolderContextInfo.ErrorInfo Error,
    ResultHolderContextInfo.IsOkInfo IsOk,
    EquatableList<HolderContainingType> ContainingTypes = default,
    ResultHolderContextInfo.ResultConfiguration Configuration = default
) {
    public readonly record struct ResultConfiguration(
        bool? ImplicitCastOkTypeToResult,
        bool? ImplicitCastErrorTypeToResult,
        bool? IncludeResultOkObject,
        bool? IncludeResultErrorObject,
        bool? PublicState,
        bool? AddCastByRefMethod,
        bool? GenerateSystemTextJsonConverter,
        bool? GenerateMessagePackFormatter
    ) {
        public ResultConfiguration MergeWithMajor(ResultConfiguration other) {
            return new(
                ImplicitCastOkTypeToResult: other.ImplicitCastOkTypeToResult ?? ImplicitCastOkTypeToResult ?? null,
                ImplicitCastErrorTypeToResult: other.ImplicitCastErrorTypeToResult ?? ImplicitCastErrorTypeToResult ?? null,
                IncludeResultOkObject: other.IncludeResultOkObject ?? IncludeResultOkObject ?? null,
                IncludeResultErrorObject: other.IncludeResultErrorObject ?? IncludeResultErrorObject ?? null,
                PublicState: other.PublicState ?? PublicState ?? null,
                AddCastByRefMethod: other.AddCastByRefMethod ?? AddCastByRefMethod ?? null,
                GenerateSystemTextJsonConverter: other.GenerateSystemTextJsonConverter ?? GenerateSystemTextJsonConverter ?? null,
                GenerateMessagePackFormatter: other.GenerateMessagePackFormatter ?? GenerateMessagePackFormatter ?? null
            );
        }

        public ResultConfiguration ApplyDefaults() {
            return new(
                ImplicitCastOkTypeToResult: ImplicitCastOkTypeToResult ?? true,
                ImplicitCastErrorTypeToResult: ImplicitCastErrorTypeToResult ?? true,
                IncludeResultOkObject: IncludeResultOkObject ?? true,
                IncludeResultErrorObject: IncludeResultErrorObject ?? true,
                PublicState: PublicState ?? false,
                AddCastByRefMethod: AddCastByRefMethod ?? false,
                GenerateSystemTextJsonConverter: GenerateSystemTextJsonConverter ?? false,
                GenerateMessagePackFormatter: GenerateMessagePackFormatter ?? false
            );
        }
    }

    public readonly record struct ResultInfo(
        string DeclarationName,
        string OnlyName,
        string GlobalName,
        TypeAccessibility Accessibility
    );

    public readonly record struct OkInfo(
        bool IsStruct,
        bool IsTypeParameter,
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
        bool IsTypeParameter,
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

static class ResultConfigurationExt {
    public static ResultHolderContextInfo.ResultConfiguration ReadResultConfigurationFromAttributes(
        this ImmutableArray<AttributeData> attributes
    ) {
        var attributeData = attributes
            .FirstOrDefault(x => x.AttributeClass?.FullPath().Equals(HolderTypeNames.ResultConfigurationFullName) ?? false);
        if (attributeData is null) {
            return default;
        }

        var configuration = new ResultHolderContextInfo.ResultConfiguration();
        foreach (var na in attributeData.NamedArguments) {
            switch (na.Key) {
                case "ImplicitCastOkTypeToResult":
                    configuration = configuration with {
                        ImplicitCastOkTypeToResult = na.Value.Value is true
                    };
                    break;
                case "ImplicitCastErrorTypeToResult":
                    configuration = configuration with {
                        ImplicitCastErrorTypeToResult = na.Value.Value is true
                    };
                    break;
                case "IncludeResultOkObject":
                    configuration = configuration with {
                        IncludeResultOkObject = na.Value.Value is true
                    };
                    break;
                case "IncludeResultErrorObject":
                    configuration = configuration with {
                        IncludeResultErrorObject = na.Value.Value is true
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

    public static IncrementalValueProvider<ResultHolderContextInfo.ResultConfiguration> ReadResultConfiguration(
        this IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider
    ) =>
        optionsProvider.Select(static (x, _) => {
                var options = x.GlobalOptions;
                ResultHolderContextInfo.ResultConfiguration configuration = default;

                if (options.TryGetBool("PerfHoldersResultImplicitCastOkTypeToResult", out var b1)) {
                    configuration = configuration with {
                        ImplicitCastOkTypeToResult = b1
                    };
                }

                if (options.TryGetBool("PerfHoldersResultImplicitCastErrorTypeToResult", out var b2)) {
                    configuration = configuration with {
                        ImplicitCastErrorTypeToResult = b2
                    };
                }

                if (options.TryGetBool("PerfHoldersResultIncludeResultOkObject", out var b3)) {
                    configuration = configuration with {
                        IncludeResultOkObject = b3
                    };
                }

                if (options.TryGetBool("PerfHoldersResultIncludeResultErrorObject", out var b4)) {
                    configuration = configuration with {
                        IncludeResultErrorObject = b4
                    };
                }

                if (options.TryGetBool("PerfHoldersResultPublicState", out var b5)) {
                    configuration = configuration with {
                        PublicState = b5
                    };
                }

                if (options.TryGetBool("PerfHoldersResultAddCastByRefMethod", out var b6)) {
                    configuration = configuration with {
                        AddCastByRefMethod = b6
                    };
                }

                if (options.TryGetBool("PerfHoldersResultGenerateJsonConverter", out var b7)) {
                    configuration = configuration with {
                        GenerateSystemTextJsonConverter = b7
                    };
                }

                if (options.TryGetBool("PerfHoldersResultGenerateMessagePackFormatter", out var b8)) {
                    configuration = configuration with {
                        GenerateMessagePackFormatter = b8
                    };
                }

                return configuration;
            }
        );
}
