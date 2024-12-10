namespace Perf.Monads;

public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable {
    public bool Equals(Unit other) => true;
    public int CompareTo(Unit other) => 0;
    public int CompareTo(object? obj) => 0;
    public override string ToString() => "<()>";
}
