// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
    MultiResultHolderContextInfo.MultiResultConfiguration Configuration = default,
    CompInfo CompInfo = default
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

    public bool ShouldGenerateJsonConverters() {
        if (CompInfo.SystemTextJsonAvailable is false) {
            return false;
        }

        if (Configuration.GenerateSystemTextJsonConverter is not true) {
            return false;
        }

        if (MultiResult.Accessibility is not TypeAccessibility.Public and not TypeAccessibility.Internal) {
            return false;
        }

        foreach (var ct in ContainingTypes) {
            if (ct.Accessibility is not TypeAccessibility.Public and not TypeAccessibility.Internal) {
                return false;
            }
        }

        return true;
    }

    public string GeneratedJsonConverterTypeForAttribute { get; } =
        $"global::Perf.Holders.Serialization.SystemTextJson.{(MultiResult.TypeParameterCount is 0 ? $"JsonConverter_{MultiResult.OnlyName}" : $"JsonConverterFactory_{MultiResult.OnlyName}")}";

    public bool ShouldGenerateMessagePackFormatters() {
        if (CompInfo.MessagePackAvailable is false) {
            return false;
        }

        if (Configuration.GenerateMessagePackFormatter is not true) {
            return false;
        }

        if (MultiResult.Accessibility is not TypeAccessibility.Public and not TypeAccessibility.Internal) {
            return false;
        }

        foreach (var ct in ContainingTypes) {
            if (ct.Accessibility is not TypeAccessibility.Public and not TypeAccessibility.Internal) {
                return false;
            }
        }

        return true;
    }

    public string GeneratedMessagePackFormatterTypeForAttribute() {
        if (MultiResult.TypeParameterCount is 0) {
            return $"global::Perf.Holders.Serialization.MessagePack.MessagePackFormatter_{MultiResult.OnlyName}";
        }

        var count = Elements.Count(x => x.IsTypeParameter);
        var buffer = ArrayPool<char>.Shared.Rent(count * 2 - 1);
        var span = buffer.AsSpan(0, count - 1);
        span.Fill(',');

        var type = $"global::Perf.Holders.Serialization.MessagePack.MessagePackFormatter_{MultiResult.OnlyName}<{span}>";

        ArrayPool<char>.Shared.Return(buffer);
        return type;
    }

    public TypeAccessibility InheritedAccessibility { get; } =
        (TypeAccessibility)Math.Max((int)MultiResult.Accessibility, (int)ContainingTypes.Max(x => x.Accessibility));
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
