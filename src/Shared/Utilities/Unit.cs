namespace Utilities;

public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable {
    private static readonly Unit ValueInstance = new();

    public static ref readonly Unit Value => ref ValueInstance;

    public static ValueTask<Unit> ValueTask => new(ValueInstance);

    public int CompareTo(Unit other) => 0;

    int IComparable.CompareTo(object? obj) => 0;

    public override int GetHashCode() => 0;

    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj) => obj is Unit;

    public static bool operator ==(Unit _, Unit __) => true;

    public static bool operator !=(Unit _, Unit __) => false;

    public override string ToString() => "()";
}
