// ReSharper disable UnusedTypeParameter

namespace Perf.Holders;

/// <summary>
/// Source Generation and Reflection Marker
/// </summary>
public interface IResultHolder<out TOk, out TError>
    where TOk : notnull
    where TError : notnull {
    // TOk Ok { get; }
    // TError Error { get; }
    // bool IsOk { get; }
}

public enum ResultState : byte {
    Uninitialized = 0,
    Ok = 1,
    Error = 2
}
