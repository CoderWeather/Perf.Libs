namespace Perf.Holders;

/// <summary>
/// Source Generation and Reflection Marker
/// </summary>
public interface IOptionHolder<out T> where T : notnull {
    T Some { get; }
    bool IsSome { get; }
}

public enum OptionState : byte {
    None = 0,
    Some = 1
}
