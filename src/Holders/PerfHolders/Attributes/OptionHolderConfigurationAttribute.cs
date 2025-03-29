// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class OptionHolderConfigurationAttribute : Attribute {
    /// <summary>
    /// Default true
    /// </summary>
    public bool ImplicitCastSomeTypeToOption { get; set; }

    /// <summary>
    /// Default true
    /// </summary>
    public bool IncludeOptionSomeObject { get; set; }
}
