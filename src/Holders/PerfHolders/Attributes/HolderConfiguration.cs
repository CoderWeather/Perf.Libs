// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ResultHolderConfigurationAttribute : Attribute {
    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastOkTypeToResult { get; init; } = true;

    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastErrorTypeToResult { get; init; } = true;

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeResultOkObject { get; init; } = true;

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeResultErrorObject { get; init; } = true;

    /// <summary>
    /// Default false
    /// </summary>
    public bool PublicState { get; init; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool AddCastByRefMethod { get; init; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateSystemTextJsonConverter { get; set; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateMessagePackFormatter { get; set; }
}

[AttributeUsage(AttributeTargets.Struct)]
public sealed class OptionHolderConfigurationAttribute : Attribute {
    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastSomeTypeToOption { get; init; } = true;

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeOptionSomeObject { get; init; } = true;

    /// <summary>
    /// Default false
    /// </summary>
    public bool PublicState { get; init; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool AddCastByRefMethod { get; init; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateSystemTextJsonConverter { get; set; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateMessagePackFormatter { get; set; }
}

[AttributeUsage(AttributeTargets.Struct)]
public sealed class MultiResultHolderConfigurationAttribute : Attribute {
    /// <summary>
    /// Default true
    /// </summary>
    public bool AddIsProperties { get; set; } = true;

    /// <summary>
    /// Default true
    /// </summary>
    public bool OpenState { get; set; } = true;

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateSystemTextJsonConverter { get; set; }

    /// <summary>
    /// Default false
    /// </summary>
    public bool GenerateMessagePackFormatter { get; set; }
}
