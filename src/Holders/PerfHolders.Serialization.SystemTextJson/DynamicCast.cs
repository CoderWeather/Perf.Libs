namespace Perf.Holders.Serialization.SystemTextJson;

using System.Linq.Expressions;

static class DynamicCast {
    public static TTo Cast<TFrom, TTo>(ref TFrom v) => Cache<TFrom, TTo>.Func(v);
    public static TempHolderFrom<TFrom> From<TFrom>(TFrom value) => new(value);

    public readonly struct TempHolderFrom<TFrom>(TFrom value) {
        public TTo To<TTo>() => Cache<TFrom, TTo>.Func(value);
    }

    static class Cache<TFrom, TTo> {
        public static readonly Func<TFrom, TTo> Func;

        static Cache() {
            var t1 = typeof(TFrom);
            var t2 = typeof(TTo);

            var p1 = Expression.Parameter(t1, "v");
            var lambda = Expression.Lambda<Func<TFrom, TTo>>(
                Expression.Convert(p1, t2),
                p1
            );

            Func = lambda.Compile();
        }
    }
}
