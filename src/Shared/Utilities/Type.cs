using System.Runtime.CompilerServices;

namespace Utilities;

public static class TypeFunctions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Type Typeof<T>() => TypeCache<T>.Type;

	private static class TypeCache<T> {
		public static readonly Type Type = typeof(T);
	}
}
