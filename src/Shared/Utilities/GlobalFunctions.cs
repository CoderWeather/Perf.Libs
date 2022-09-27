using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Utilities;

public static class GlobalFunctions {
	[ThreadStatic]
	private static Random? RandomPerThread;

	private static Random Random => RandomPerThread ??= new();

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Guid NewGuid() => GuidPerf.New();

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long NewLong() => Random.NextInt64();

	public static Expression<Func<T>> Expr<T>(Expression<Func<T>> lambda) => lambda;
	public static Expression<Func<T, T>> Expr<T>(Expression<Func<T, T>> lambda) => lambda;
}
