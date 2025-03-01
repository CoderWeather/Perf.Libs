using System.Collections.Immutable;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Utilities;

public static class CollectionHacks {
#region Stack

    public static bool TryPopWhen<T>(this Stack<T> stack, Func<T, bool> predicate, out T el) {
        if (stack.Count > 0) {
            var span = stack.AsSpan();
            var last = span[^1];

            if (predicate.Invoke(last)) {
                el = stack.Pop();
                return true;
            }
        }

        el = default!;
        return false;
    }

    public static bool TryPeekWhen<T>(this Stack<T> stack, Func<T, bool> predicate, out T el) {
        if (stack.Count > 0) {
            var span = stack.AsSpan();
            var last = span[^1];

            if (predicate.Invoke(last)) {
                el = last;
                return true;
            }
        }

        el = default!;
        return false;
    }

    public static ReadOnlySpan<T> AsSpan<T>(this Stack<T> stack) {
        var array = StackCache<T>.GetArray(stack);
        var size = StackCache<T>.GetSize(stack);
        return array.AsSpan(0, size);
    }

    private static class StackCache<T> {
        public static readonly Func<Stack<T>, int> GetSize;
        public static readonly Func<Stack<T>, T[]> GetArray;

        static StackCache() {
            var t = typeof(Stack<T>);
            var stackParameter = Expression.Parameter(t);

            var sizeField = t.GetRuntimeFields().First(x => x.Name is "_size");
            var getSizeLambda = Expression.Lambda<Func<Stack<T>, int>>(
                Expression.Field(stackParameter, sizeField),
                stackParameter
            );
            GetSize = getSizeLambda.CompileFast();

            var arrayField = t.GetRuntimeFields().First(x => x.Name is "_array");
            var getArrayLambda = Expression.Lambda<Func<Stack<T>, T[]>>(
                Expression.Field(stackParameter, arrayField),
                stackParameter
            );
            GetArray = getArrayLambda.CompileFast();
        }
    }

#endregion

#region Immutable Array

    public static T[] UnWrapArray<T>(this ImmutableArray<T> ia) => ImmutableArrayCache<T>.FuncArrayAccess.Invoke(ia);

    public static ImmutableArray<T> WrapAsImmutable<T>(this T[] a) => ImmutableArrayCache<T>.FuncWrap.Invoke(a);

    private static class ImmutableArrayCache<T> {
        public static readonly Func<ImmutableArray<T>, T[]> FuncArrayAccess;
        public static readonly Func<T[], ImmutableArray<T>> FuncWrap;

        static ImmutableArrayCache() {
            var param = Expression.Parameter(typeof(ImmutableArray<T>), "ia");
            var lambda = Expression.Lambda<Func<ImmutableArray<T>, T[]>>(
                Expression.MakeMemberAccess(
                    param,
                    typeof(ImmutableArray<T>).GetRuntimeFields().First(x => x.Name is "array")
                ),
                param
            );

            FuncArrayAccess = lambda.Compile();

            param = Expression.Parameter(typeof(T[]), "a");
            var lambda2 = Expression.Lambda<Func<T[], ImmutableArray<T>>>(
                Expression.New(
                    typeof(ImmutableArray<T>)
                       .GetConstructor(
                            BindingFlags.NonPublic | BindingFlags.Instance,
                            [
                                typeof(T).MakeArrayType()
                            ]
                        )!,
                    param
                ),
                param
            );

            FuncWrap = lambda2.Compile();
        }
    }

#endregion
}
