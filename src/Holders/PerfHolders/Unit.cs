// ReSharper disable UnusedParameter.Global

namespace Perf.Holders;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 1)]
public readonly struct Unit : IEquatable<Unit> {
    public static Unit Value => default;
    public override bool Equals(object? obj) => obj is Unit;
    public bool Equals(Unit other) => true;
    public override int GetHashCode() => 0;
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
    public override string ToString() => "()";
}
