using System.Runtime.CompilerServices;

namespace Utilities;

public static class EnumerableExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] AsArray<T>(this IEnumerable<T> enumerable) => enumerable as T[] ?? enumerable.ToArray();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static List<T> AsList<T>(this IEnumerable<T> enumerable) => enumerable as List<T> ?? enumerable.ToList();
}
