// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Perf.Holders.Generator.Types;

using System.Collections;
using Internal;

readonly struct EquatableList<T>(List<T> list) :
    IEquatable<EquatableList<T>>,
    IList<T>
    where T : IEquatable<T> {
    public EquatableList() : this([ ]) { }
    public EquatableList(int size) : this(new List<T>(size)) { }

    public ReadOnlySpan<T> Span =>
        list != null!
            ? list.GetUnderlyingArray().AsSpan(0, list.Count)
            : default;

    public int Count => Span.Length;

    public override bool Equals(object? obj) => obj is EquatableList<T> other && Equals(other);

    public override int GetHashCode() {
        HashCode hc = default;
        foreach (var item in this) {
            hc.Add(item);
        }

        return hc.ToHashCode();
    }

    public bool Equals(EquatableList<T> other) => Span.SequenceEqual(other.Span);
    public static bool operator ==(EquatableList<T> left, EquatableList<T> right) => left.Equals(right);
    public static bool operator !=(EquatableList<T> left, EquatableList<T> right) => left.Equals(right) is false;
    public ReadOnlySpan<T>.Enumerator GetEnumerator() => Span.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    public void Add(T item) => list.Add(item);
    public void Clear() => list.Clear();
    public bool Contains(T item) => list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => Span.CopyTo(array.AsSpan(arrayIndex));
    public bool Remove(T item) => list.Remove(item);
    bool ICollection<T>.IsReadOnly => false;
    public int IndexOf(T item) => list.IndexOf(item);
    public void Insert(int index, T item) => list.Insert(index, item);
    public void RemoveAt(int index) => list.RemoveAt(index);
    public T this[int index] { get => list[index]; set => list[index] = value; }

    public bool Any(Func<T, bool> predicate) {
        foreach (ref readonly var item in Span) {
            if (predicate(item)) {
                return true;
            }
        }

        return false;
    }
}
