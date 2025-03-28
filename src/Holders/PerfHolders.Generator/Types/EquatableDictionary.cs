namespace Perf.Holders.Generator.Types;

using Internal;

sealed class EquatableDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
    IEquatable<EquatableDictionary<TKey, TValue>>
    where TKey : notnull {
    public bool Equals(EquatableDictionary<TKey, TValue>? other) {
        if (other is null || this.Count != other.Count) {
            return false;
        }

        foreach (var p in this) {
            if (other.TryGetValue(p.Key, out var otherValue) is false
                || EqualityComparer<TValue>.Default.Equals(p.Value, otherValue) is false) {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() {
        HashCode hc = default;
        foreach (var p in this) {
            hc.Add(p.Key);
            hc.Add(p.Value);
        }

        return hc.ToHashCode();
    }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || obj is EquatableDictionary<TKey, TValue> other && Equals(other);

    public static bool operator ==(EquatableDictionary<TKey, TValue>? left, EquatableDictionary<TKey, TValue>? right) => Equals(left, right);
    public static bool operator !=(EquatableDictionary<TKey, TValue>? left, EquatableDictionary<TKey, TValue>? right) => Equals(left, right) is false;
}
