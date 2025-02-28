namespace Perf.Holders.Internal;

using System.Linq.Expressions;

static class DynamicCast {
    public static TTo Cast<TFrom, TTo>(ref TFrom v) => Cache<TFrom, TTo>.Caster(ref v);

    delegate TTo Caster<TFrom, out TTo>(ref TFrom v);

    static class Cache<TFrom, TTo> {
        public static readonly Caster<TFrom, TTo> Caster;

        static Cache() {
            var t1 = typeof(TFrom);
            var t2 = typeof(TTo);

            var p1 = Expression.Parameter(t1.MakeByRefType(), "v");
            var lambda = Expression.Lambda<Caster<TFrom, TTo>>(
                Expression.Convert(p1, t2),
                p1
            );

            Caster = lambda.Compile();
        }
    }
}
