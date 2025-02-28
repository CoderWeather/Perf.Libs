// ReSharper disable UnusedParameter.Global

namespace Perf.Holders;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable {
    public static Unit Value => default;
    public override bool Equals(object? obj) => obj is Unit;
    public bool Equals(Unit other) => true;
    public override int GetHashCode() => 0;
    public int CompareTo(Unit other) => 0;

    public int CompareTo(object? obj) =>
        obj is Unit ? 0 : throw new ArgumentException("Cannot compare Unit with any type other than Unit", nameof(obj));

    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;

    public override string ToString() => "()";
}
