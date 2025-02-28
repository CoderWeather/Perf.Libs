namespace Perf.Holders;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
// ReSharper disable once InconsistentNaming
public static class ___HoldersInvisibleHelpers {
    public static TOther Cast<TOk, TError, TOther>(ref Result<TOk, TError> r)
        where TOther : struct, IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull {
        var other = DynamicCast.Cast<Result<TOk, TError>, TOther>(ref r);
        return other;
    }

    public static TOther Cast<T, TOk, TError, TOther>(ref T result)
        where T : struct, IResultHolder<TOk, TError>
        where TOther : struct, IResultHolder<TOk, TError>
        where TOk : notnull
        where TError : notnull {
        var other = Unsafe.As<T, TOther>(ref result);
        return other;
    }
}
