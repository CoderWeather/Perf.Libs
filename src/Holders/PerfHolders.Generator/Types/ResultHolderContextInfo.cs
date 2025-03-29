namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

readonly record struct ResultHolderContextInfo(
    string SourceFileName,
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
        bool? IncludeResultErrorObject
    ) {
        public ResultConfiguration MergeWithMajor(ResultConfiguration other) {
            return new() {
                ImplicitCastOkTypeToResult = other.ImplicitCastOkTypeToResult ?? ImplicitCastOkTypeToResult ?? null,
                ImplicitCastErrorTypeToResult = other.ImplicitCastErrorTypeToResult ?? ImplicitCastErrorTypeToResult ?? null,
                IncludeResultOkObject = other.IncludeResultOkObject ?? IncludeResultOkObject ?? null,
                IncludeResultErrorObject = other.IncludeResultErrorObject ?? IncludeResultErrorObject ?? null
            };
        }

        public ResultConfiguration ApplyDefaults() {
            return new() {
                ImplicitCastOkTypeToResult = ImplicitCastOkTypeToResult ?? true,
                ImplicitCastErrorTypeToResult = ImplicitCastErrorTypeToResult ?? true,
                IncludeResultOkObject = IncludeResultOkObject ?? true,
                IncludeResultErrorObject = IncludeResultErrorObject ?? true
            };
        }
    }

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
                if (options.TryGetValue("PerfHoldersResultImplicitCastOkTypeToResult", out var s1) && s1 is not null and not "") {
                    configuration = configuration with {
                        ImplicitCastOkTypeToResult =
                        s1.Equals("enable", StringComparison.OrdinalIgnoreCase) || s1.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s1.Equals("disable", StringComparison.OrdinalIgnoreCase) || s1.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                if (options.TryGetValue("PerfHoldersResultImplicitCastErrorTypeToResult", out var s2) && s2 is not null and not "") {
                    configuration = configuration with {
                        ImplicitCastErrorTypeToResult =
                        s2.Equals("enable", StringComparison.OrdinalIgnoreCase) || s2.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s2.Equals("disable", StringComparison.OrdinalIgnoreCase) || s2.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                if (options.TryGetValue("PerfHoldersResultIncludeResultOkObject", out var s3) && s3 is not null and not "") {
                    configuration = configuration with {
                        IncludeResultOkObject =
                        s3.Equals("enable", StringComparison.OrdinalIgnoreCase) || s3.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s3.Equals("disable", StringComparison.OrdinalIgnoreCase) || s3.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                if (options.TryGetValue("PerfHoldersResultIncludeResultErrorObject", out var s4) && s4 is not null and not "") {
                    configuration = configuration with {
                        IncludeResultErrorObject =
                        s4.Equals("enable", StringComparison.OrdinalIgnoreCase) || s4.Equals("true", StringComparison.OrdinalIgnoreCase)   ? true :
                        s4.Equals("disable", StringComparison.OrdinalIgnoreCase) || s4.Equals("false", StringComparison.OrdinalIgnoreCase) ? false : null
                    };
                }

                return configuration;
            }
        );
}
