namespace Perf.Holders.Generator;

static class HolderTypeNames {
    public const string ResultMarkerName = "IResultHolder";
    public const string ResultMarkerFullName = "Perf.Holders.IResultHolder";
    public const string ResultConfigurationFullName = "Perf.Holders.Attributes.ResultHolderConfigurationAttribute";
    public const string ResultSerializationSystemTextJson = "Perf.Holders.Serialization.SystemTextJson.ResultHolderJsonConverterFactory";
    public const string ResultSerializationMessagePack = "Perf.Holders.Serialization.MessagePack.ResultHolderFormatterResolver";

    public const string OptionMarkerName = "IOptionHolder";
    public const string OptionMarkerFullName = "Perf.Holders.IOptionHolder";
    public const string OptionConfigurationFullName = "Perf.Holders.Attributes.OptionHolderConfigurationAttribute";
    public const string OptionSerializationSystemTextJson = "Perf.Holders.Serialization.SystemTextJson.OptionHolderJsonConverterFactory";
    public const string OptionSerializationMessagePack = "Perf.Holders.Serialization.MessagePack.OptionHolderFormatterResolver";
}
