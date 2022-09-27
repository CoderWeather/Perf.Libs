using System.Runtime.CompilerServices;

namespace Utilities;

public static class ArrayUnsafe {
	public static TOut[] MapCast<TIn, TOut>(TIn[] array) {
		var ar2 = new TOut[array.Length];
		for (var i = 0; i < array.Length; i++) {
			var el = array[i];
			ar2[i] = Unsafe.As<TIn, TOut>(ref el);
		}

		return ar2;
	}
}
