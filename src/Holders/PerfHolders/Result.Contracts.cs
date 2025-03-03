// ReSharper disable UnusedTypeParameter

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
/// partial struct MyResult : IResultHolder&lt;int, string&gt;;
/// MyResult result = 10;
/// int ok = result.Ok; // 10
/// bool isOk = result.IsOk; // true
/// result = "text";
/// string error = result.Error; // "text"
/// isOk = result.IsOk; // false
/// </code>
/// <example>
/// <para>With name overriden properties</para>
/// <code>
/// partial struct MyResult : IResultHolder&lt;int, string&gt; {
///     public partial int MyOk { get; }
///     public partial string MyError { get; }
///     public partial bool MyIsOk { get; }
/// }
/// MyResult result = 10;
/// int ok = result.MyOk; // 10
/// bool isOk = result.MyIsOk; // true
/// result = "text";
/// string error = result.MyError; // "text"
/// isOk = result.MyIsOk; // false
/// </code>
/// </example>
/// <para>If not provided named property of IsOk but provided name of Ok, IsOk will be renamed to 'Is+&lt;new Ok property name&gt;'</para>
/// <code>
/// partial struct MyResult : IResultHolder&lt;int, string&gt; {
///     public partial int MyOk { get; }
/// }
/// MyResult result = 10;
/// int ok = result.MyOk; // 10
/// bool isOk = result.IsMyOk; // true
/// </code>
/// </example>
public interface IResultHolder<out TOk, out TError>
    where TOk : notnull
    where TError : notnull {
    TOk Ok { get; }
    TError Error { get; }
    bool IsOk { get; }
}

public enum ResultState : byte {
    Uninitialized = 0,
    Ok = 1,
    Error = 2
}
