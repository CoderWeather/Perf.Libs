// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ResultHolderConfigurationAttribute : Attribute {
    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastOkTypeToResult { get; set; }

    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastErrorTypeToResult { get; set; }

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeResultOkObject { get; set; }

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeResultErrorObject { get; set; }
}
