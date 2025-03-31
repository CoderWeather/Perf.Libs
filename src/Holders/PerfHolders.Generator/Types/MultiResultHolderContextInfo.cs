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
        bool? AddJsonConverterAttribute,
        bool? AddMessagePackFormatterAttribute
    ) {
        public MultiResultConfiguration MergeWithMajor(MultiResultConfiguration other) {
            return new() {
                AddIsProperties = other.AddIsProperties ?? AddIsProperties ?? null,
                OpenState = other.OpenState ?? OpenState ?? null,
                AddJsonConverterAttribute = other.AddJsonConverterAttribute ?? AddJsonConverterAttribute ?? null,
                AddMessagePackFormatterAttribute = other.AddMessagePackFormatterAttribute ?? AddMessagePackFormatterAttribute ?? null
            };
        }

        public MultiResultConfiguration ApplyDefaults() {
            return new() {
                AddIsProperties = AddIsProperties ?? true,
                OpenState = OpenState ?? true,
                AddJsonConverterAttribute = AddJsonConverterAttribute ?? false,
                AddMessagePackFormatterAttribute = AddMessagePackFormatterAttribute ?? false
            };
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

    public string TypeParametersConstraints() {
        if (MultiResult.TypeParameterCount is 0) {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var el in Elements) {
            if (el.IsTypeParameter) {
                sb.AppendInterpolatedLine($"where {el.Type} : notnull");
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
                case "AddJsonConverterAttribute":
                    configuration = configuration with {
                        AddJsonConverterAttribute = na.Value.Value is true
                    };
                    break;
                case "AddMessagePackFormatterAttribute":
                    configuration = configuration with {
                        AddMessagePackFormatterAttribute = na.Value.Value is true
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

                if (options.TryGetBool("PerfHoldersMultiResultAddJsonConverterAttribute", out var b3)) {
                    configuration = configuration with {
                        AddJsonConverterAttribute = b3
                    };
                }

                if (options.TryGetBool("PerfHoldersMultiResultAddMessagePackFormatterAttribute", out var b4)) {
                    configuration = configuration with {
                        AddMessagePackFormatterAttribute = b4
                    };
                }

                return configuration;
            }
        );
}
