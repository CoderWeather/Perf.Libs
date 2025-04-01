// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using System.Collections.Immutable;
using System.Text;
using Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

readonly record struct MultiResultHolderContextInfo(
    string MetadataName,
    string? Namespace,
    MultiResultHolderContextInfo.MultiResultInfo MultiResult,
    EquatableList<MultiResultHolderContextInfo.MultiResultElementInfo> Elements,
    EquatableList<HolderContainingType> ContainingTypes = default,
    MultiResultHolderContextInfo.MultiResultConfiguration Configuration = default
) {
    public readonly record struct MultiResultConfiguration(
        bool? AddIsProperties,
        bool? OpenState,
        bool? GenerateSystemTextJsonConverter,
        bool? GenerateMessagePackFormatter
    ) {
        public MultiResultConfiguration MergeWithMajor(MultiResultConfiguration other) {
            return new(
                AddIsProperties: other.AddIsProperties ?? AddIsProperties ?? null,
                OpenState: other.OpenState ?? OpenState ?? null,
                GenerateSystemTextJsonConverter: other.GenerateSystemTextJsonConverter ?? GenerateSystemTextJsonConverter ?? null,
                GenerateMessagePackFormatter: other.GenerateMessagePackFormatter ?? GenerateMessagePackFormatter ?? null
            );
        }

        public MultiResultConfiguration ApplyDefaults() {
            return new(
                AddIsProperties: AddIsProperties ?? true,
                OpenState: OpenState ?? true,
                GenerateSystemTextJsonConverter: GenerateSystemTextJsonConverter ?? false,
                GenerateMessagePackFormatter: GenerateMessagePackFormatter ?? false
            );
        }
    }

    public readonly record struct MultiResultInfo(
        string DeclarationName,
        string OnlyName,
        string GlobalName,
        int TypeParameterCount,
        TypeAccessibility Accessibility
    );

    public readonly record struct MultiResultElementInfo(
        int Index,
        bool IsStruct,
        bool IsTypeParameter,
        string Property,
        string Field,
        string Type,
        string TypeNullable,
        bool HavePartial,
        MultiResultElementStateCheckInfo StateCheck
    ) {
        public static ImmutableArray<string> Properties => [ "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth" ];
        public static ImmutableArray<string> Fields => [ "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth" ];

        public string DefaultProperty => Properties[Index];
        public string DefaultField => Fields[Index];
    }

    public readonly record struct MultiResultElementStateCheckInfo(
        string Property,
        bool HavePartial
    );

    /// <summary>
    /// &lt;,,,&gt;
    /// </summary>
    /// <returns></returns>
    public string OpenTypeParameters() {
        if (MultiResult.TypeParameterCount is 0) {
            return "";
        }

        var sb = new StringBuilder(2 + MultiResult.TypeParameterCount - 1);

        sb.Append('<');
        for (var i = 0; i < MultiResult.TypeParameterCount; i++) {
            sb.Append(',');
        }

        sb.Length--;
        sb.Append('>');

        return sb.ToString();
    }

    /// <summary>
    /// &lt;T1,T2,T3&gt;
    /// </summary>
    /// <returns></returns>
    public string TypeParameters() {
        if (MultiResult.TypeParameterCount is 0) {
            return "";
        }

        var sb = new StringBuilder(2 + MultiResult.TypeParameterCount * 2 - 1);

        sb.Append('<');
        foreach (var el in Elements) {
            if (el.IsTypeParameter) {
                sb.AppendInterpolated($"{el.Type},");
            }
        }

        sb.Length--;
        sb.Append('>');

        return sb.ToString();
    }

    /// <summary>
    /// where T1 : notnull \n where T2 : notnull
    /// </summary>
    /// <returns></returns>
    public string TypeParametersConstraints(char delimiter = '\n') {
        if (MultiResult.TypeParameterCount is 0) {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var el in Elements) {
            if (el.IsTypeParameter) {
                sb.AppendInterpolated($"where {el.Type} : notnull");
                sb.Append(delimiter);
            }
        }

        return sb.ToString();
    }
}

static class MultiResultConfigurationExt {
    public static MultiResultHolderContextInfo.MultiResultConfiguration ReadMultiResultConfigurationFromAttributes(
        this ImmutableArray<AttributeData> attributes
    ) {
        var attributeData = attributes
            .FirstOrDefault(x => x.AttributeClass?.FullPath().Equals(HolderTypeNames.MultiResultConfigurationFullName) ?? false);
        if (attributeData is null) {
            return default;
        }

        var configuration = new MultiResultHolderContextInfo.MultiResultConfiguration();
        foreach (var na in attributeData.NamedArguments) {
            switch (na.Key) {
                case "AddIsProperties":
                    configuration = configuration with {
                        AddIsProperties = na.Value.Value is true
                    };
                    break;
                case "OpenState":
                    configuration = configuration with {
                        OpenState = na.Value.Value is true
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

    public static IncrementalValueProvider<MultiResultHolderContextInfo.MultiResultConfiguration> ReadMultiResultConfiguration(
        this IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider
    ) =>
        optionsProvider.Select(static (x, _) => {
                var options = x.GlobalOptions;
                MultiResultHolderContextInfo.MultiResultConfiguration configuration = default;

                if (options.TryGetBool("PerfHoldersMultiResultAddIsProperties", out var b1)) {
                    configuration = configuration with {
                        AddIsProperties = b1
                    };
                }

                if (options.TryGetBool("PerfHoldersMultiResultOpenState", out var b2)) {
                    configuration = configuration with {
                        OpenState = b2
                    };
                }

                if (options.TryGetBool("PerfHoldersMultiResultGenerateJsonConverter", out var b4)) {
                    configuration = configuration with {
                        GenerateSystemTextJsonConverter = b4
                    };
                }

                if (options.TryGetBool("PerfHoldersMultiResultGenerateMessagePackFormatter", out var b6)) {
                    configuration = configuration with {
                        GenerateMessagePackFormatter = b6
                    };
                }

                return configuration;
            }
        );
}
