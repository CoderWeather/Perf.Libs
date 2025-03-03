namespace Perf.Holders;

using System.ComponentModel;
using System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
// ReSharper disable once InconsistentNaming
public static class ___HoldersInvisibleHelpers {
    public static ref TOther CastResult<T, TOk, TError, TOther>(ref T result)
        where T : struct, IResultHolder<TOk, TError>
        where TOther : struct, IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull {
        ref var other = ref Unsafe.As<T, TOther>(ref result);
        return ref other;
    }

    public static ref TOther CastOption<T, TSome, TOther>(ref T option)
        where T : struct, IOptionHolder<TSome>
        where TOther : struct, IOptionHolder<TSome>
        where TSome : notnull {
        ref var other = ref Unsafe.As<T, TOther>(ref option);
        return ref other;
    }
}
