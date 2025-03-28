// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Perf.Holders.Generator.Types;

using System.Collections;
using Internal;

readonly struct EquatableArray<T>(T[] array) :
    IEquatable<EquatableArray<T>>,
    IEnumerable<T>
    where T : IEquatable<T> {
    public Span<T> Span => array;
    public int Count => array.Length;
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode() {
        HashCode hc = default;
        foreach (var item in this) {
            hc.Add(item);
        }

        return hc.ToHashCode();
    }

    public bool Equals(EquatableArray<T> other) => Span.SequenceEqual(other.Span);
    public Span<T>.Enumerator GetEnumerator() => array.AsSpan().GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>)array.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();
}
