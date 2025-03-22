namespace Perf.Holders;

/// <summary>
/// Source Generation and Reflection Marker
/// </summary>
/// <remarks>
/// <para>You can rename generated properties via <see langword="partial"/> properties with specified types from interface marker specification</para>
/// <para>For not provided names default names will be used</para>
/// <para>If you using bool for Some value it will be named in order Ok->Error->IsOk</para>
/// </remarks>
/// <example>
/// <para>With default generated properties</para>
/// <code>
/// partial struct MyOption : IOptionHolder&lt;string&gt;;
/// MyOption opt = "text";
/// int ok = opt.Some; // "text"
/// bool isSome = opt.IsSome; // true
/// opt = default;
/// isSome = opt.IsSome; // false
/// </code>
/// <example>
/// <para>With name overriden properties</para>
/// <code>
/// partial struct MyOption : IOptionHolder&lt;string&gt; {
///     public partial string MySome { get; }
///     public partial bool MyIsSome { get; }
/// }
/// MyOption opt = "text";
/// int ok = opt.MySome; // "text"
/// bool isSome = opt.MyIsSome; // true
/// opt = default;
/// isSome = opt.MyIsSome; // false
/// </code>
/// </example>
/// <para>If not provided named property of IsSome but provided name of Some, IsSome will be renamed to 'Is+&lt;new Some property name&gt;'</para>
/// <code>
/// partial struct MyOption : IOptionHolder&lt;string&gt; {
///     public partial string MySome { get; }
/// }
/// MyOption opt = "text";
/// bool isSome = opt.IsMySome; // true
/// </code>
/// </example>
public interface IOptionHolder<out T> where T : notnull {
    T Some { get; }
    bool IsSome { get; }
}

public enum OptionState : byte {
    None = 0,
    Some = 1
}
