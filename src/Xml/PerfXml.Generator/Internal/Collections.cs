namespace PerfXml.Generator.Internal;

using System.Linq.Expressions;
using System.Runtime.CompilerServices;

public static class EnumerableExtensions {
    public static T[] AsArray<T>(this IEnumerable<T> enumerable) => enumerable as T[] ?? enumerable.ToArray();
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

    public static T? Find<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate) {
        foreach (var i in span) {
            if (predicate.Invoke(i)) {
                return i;
            }
        }

        return default;
    }

    public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> enumerable) {
        foreach (var i in enumerable) {
            set.Add(i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Span<T>(this List<T> list) => ListCache<T>.GetItems(list).AsSpan().Slice(0, list.Count);

    static class ListCache<T> {
        public static readonly Func<List<T>, T[]> GetItems;

        static ListCache() {
            var list = Expression.Parameter(typeof(List<T>));

            var lambda = Expression.Lambda<Func<List<T>, T[]>>(
                Expression.Field(list, "_items"),
                list
            );
            GetItems = lambda.Compile();
        }
    }

    public static bool All<T>(this Span<T> span, Func<T, bool> predicate) {
        foreach (var i in span) {
            if (predicate.Invoke(i) is false) {
                return false;
            }
        }

        return true;
    }

    public static void AddIfAll<T>(this List<T> list, T item, Func<T, T, bool> predicate) {
        if (list.Span().All(x => predicate.Invoke(x, item))) {
            return;
        }

        list.Add(item);
    }

    public static void AddRangeIfAll<T>(this List<T> list, IEnumerable<T> items, Func<T, T, bool> predicate) {
        foreach (var i in items) {
            list.AddIfAll(i, predicate);
        }
    }
}
