// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Attributes;

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
}
