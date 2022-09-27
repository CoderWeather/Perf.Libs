using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Utilities;

public static class CollectionExtensions {
	public static TOut[] Map<TIn, TOut>(this TIn[] array, Func<TIn, TOut> map) {
		var result = new TOut[array.Length];
		var resSpan = result.AsSpan();
		var arSpan = array.AsSpan();
		for (var i = 0; i < arSpan.Length; i++)
			resSpan[i] = map.Invoke(arSpan[i]);

		return result;
	}

	public static int FindIndex<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate) {
		if (span.Length is 0) {
			return -1;
		}

		var length = span.Length;
		nint index = 0;
		ref var ptr = ref MemoryMarshal.GetReference(span);

		while (length > 0) {
			if (predicate.Invoke(Unsafe.Add(ref ptr, index))) {
				return (int)index;
			}

			index += 1;
			length--;
		}

		return -1;
	}

	public static int FindLastIndex<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate) {
		if (span.Length is 0) {
			return -1;
		}

		var length = span.Length;
		nint index = 0;
		ref var ptr = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), -length + 1);

		while (length >= 0) {
			if (predicate.Invoke(Unsafe.Add(ref ptr, index))) {
				return (int)-index;
			}

			index -= 1;
			length--;
		}

		return -1;
	}
}
