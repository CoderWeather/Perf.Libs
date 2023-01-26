namespace Perf.ValueObjects.Generator.Internal;

public static class EnumerableExtensions {
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable, IEqualityComparer<T>? comparer = null) => new(enumerable, comparer);

    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> enumerable,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null
    ) {
        using var enumerator = enumerable.GetEnumerator();

        if (enumerator.MoveNext()) {
            var set = new HashSet<TKey>(comparer);
            do {
                var element = enumerator.Current;
                if (set.Add(keySelector(element))) {
                    yield return element;
                }
            }
            while (enumerator.MoveNext());
        }
    }

    public static bool Contains<T>(this T?[] array, T? item) where T : notnull {
        foreach (var i in array.AsSpan()) {
            if (i?.Equals(item) is true) {
                return true;
            }
        }

        return false;
    }
}
