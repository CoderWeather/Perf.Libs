// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable MethodOverloadWithOptionalParameter

namespace System.Runtime.CompilerServices;

using Buffers;
using Diagnostics.CodeAnalysis;
using Globalization;

[InterpolatedStringHandler]
public ref struct DefaultInterpolatedStringHandler {
    readonly IFormatProvider? formatProvider;
    char[]? arrayToReturnToPool;
    Span<char> chars;
    int pos;
    readonly bool hasCustomFormatter;

    public DefaultInterpolatedStringHandler(int literalLength, int formattedCount) {
        formatProvider = null;
        chars = (Span<char>)(arrayToReturnToPool = ArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount)));
        pos = 0;
        hasCustomFormatter = false;
    }

    public DefaultInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider
    ) {
        this.formatProvider = formatProvider;
        chars = (Span<char>)(arrayToReturnToPool = ArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount)));
        pos = 0;
        hasCustomFormatter = formatProvider != null && HasCustomFormatter(formatProvider);
    }

    public DefaultInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider,
        Span<char> initialBuffer
    ) {
        this.formatProvider = formatProvider;
        chars = initialBuffer;
        arrayToReturnToPool = null;
        pos = 0;
        hasCustomFormatter = formatProvider != null && HasCustomFormatter(formatProvider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetDefaultLength(int literalLength, int formattedCount) {
        return Math.Max(256 /*0x0100*/, literalLength + formattedCount * 11);
    }

    public override string ToString() => chars[..pos].ToString();
    internal Span<char> Text => chars[..pos];

    public string ToStringAndClear() {
        var stringAndClear = ToString();
        Clear();
        return stringAndClear;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear() {
        var ar = arrayToReturnToPool;
        this = new();
        if (ar == null) {
            return;
        }

        ArrayPool<char>.Shared.Return(ar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (value.AsSpan().TryCopyTo(chars[pos..])) {
            pos += value.Length;
        } else {
            GrowThenCopyString(value);
        }
    }

    public void AppendFormatted<T>(T? value, string? format) {
        if (hasCustomFormatter) {
            AppendCustomFormatter(value, format);
        } else {
            string? str1;
            if ((object?)value is IFormattable) {
                str1 = ((IFormattable)value).ToString(format, formatProvider);
            } else {
                ref var local1 = ref value;
                string? str2;
                if (default(T) == null) {
                    var obj = local1;
                    ref var local2 = ref obj;
                    if (obj == null) {
                        str2 = null;
                        goto label_17;
                    }

                    local1 = local2;
                }

                str2 = local1?.ToString();
                label_17:
                str1 = str2;
            }

            if (str1 == null) {
                return;
            }

            AppendLiteral(str1);
        }
    }

    public void AppendFormatted<T>(T? value, int alignment) {
        var p = pos;
        AppendFormatted(value);
        if (alignment == 0) {
            return;
        }

        AppendOrInsertAlignmentIfNeeded(p, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string? format) {
        var p = pos;
        AppendFormatted(value, format);
        if (alignment == 0) {
            return;
        }

        AppendOrInsertAlignmentIfNeeded(p, alignment);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value) {
        if (value.TryCopyTo(chars[pos..])) {
            pos += value.Length;
        } else {
            GrowThenCopySpan(value);
        }
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) {
        var flag = false;
        if (alignment < 0) {
            flag = true;
            alignment = -alignment;
        }

        var length = alignment - value.Length;
        if (length <= 0) {
            AppendFormatted(value);
        } else {
            EnsureCapacityForAdditionalChars(value.Length + length);
            if (flag) {
                value.CopyTo(chars[pos..]);
                pos += value.Length;
                chars.Slice(pos, length).Fill(' ');
                pos += length;
            } else {
                chars.Slice(pos, length).Fill(' ');
                pos += length;
                value.CopyTo(chars[pos..]);
                pos += value.Length;
            }
        }
    }

    public void AppendFormatted(string? value) {
        if (! hasCustomFormatter && value != null && value.AsSpan().TryCopyTo(chars[pos..])) {
            pos += value.Length;
        } else {
            AppendFormattedSlow(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void AppendFormattedSlow(string? value) {
        if (hasCustomFormatter) {
            AppendCustomFormatter(value, null);
        } else {
            if (value == null) {
                return;
            }

            EnsureCapacityForAdditionalChars(value.Length);
            value.AsSpan().CopyTo(chars[pos..]);
            pos += value.Length;
        }
    }

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and
    /// the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    public void AppendFormatted(string? value, int alignment = 0, string? format = null) {
        AppendFormatted<string>(value, alignment, format);
    }

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and
    /// the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    public void AppendFormatted(object? value, int alignment = 0, string? format = null) {
        AppendFormatted<object>(value, alignment, format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasCustomFormatter(IFormatProvider provider) {
        return provider.GetType() != typeof(CultureInfo) && provider.GetFormat(typeof(ICustomFormatter)) != null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void AppendCustomFormatter<T>(T value, string? format) {
        var format1 = (ICustomFormatter?)formatProvider?.GetFormat(typeof(ICustomFormatter));
        var str = format1?.Format(format, value, formatProvider);
        if (str == null) {
            return;
        }

        AppendLiteral(str);
    }

    void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment) {
        var length = pos - startingPos;
        var flag = false;
        if (alignment < 0) {
            flag = true;
            alignment = -alignment;
        }

        var num = alignment - length;
        if (num <= 0) {
            return;
        }

        EnsureCapacityForAdditionalChars(num);
        if (flag) {
            chars.Slice(pos, num).Fill(' ');
        } else {
            var span = chars.Slice(startingPos, length);
            span.CopyTo(chars[(startingPos + num)..]);
            span = chars.Slice(startingPos, num);
            span.Fill(' ');
        }

        pos += num;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureCapacityForAdditionalChars(int additionalChars) {
        if (chars.Length - pos >= additionalChars) {
            return;
        }

        Grow(additionalChars);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void GrowThenCopyString(string value) {
        Grow(value.Length);
        value.AsSpan().CopyTo(chars[pos..]);
        pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void GrowThenCopySpan(scoped ReadOnlySpan<char> value) {
        Grow(value.Length);
        value.CopyTo(chars[pos..]);
        pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Grow(int additionalChars) => GrowCore((uint)(pos + additionalChars));

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Grow() => GrowCore((uint)(chars.Length + 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void GrowCore(uint requiredMinCapacity) {
        var destination = ArrayPool<char>.Shared.Rent(
            (int)Clamp(Math.Max(requiredMinCapacity, Math.Min((uint)(chars.Length * 2), 1073741791U)), 256U /*0x0100*/, int.MaxValue)
        );
        chars[..pos].CopyTo((Span<char>)destination);
        var arrayToReturnToPool = this.arrayToReturnToPool;
        chars = (Span<char>)(this.arrayToReturnToPool = destination);
        if (arrayToReturnToPool == null) {
            return;
        }

        ArrayPool<char>.Shared.Return(arrayToReturnToPool);
    }

    /// <summary>Returns <paramref name="value" /> clamped to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</summary>
    /// <param name="value">The value to be clamped.</param>
    /// <param name="min">The lower bound of the result.</param>
    /// <param name="max">The upper bound of the result.</param>
    /// <returns>
    ///        <paramref name="value" /> if <paramref name="min" /> ≤ <paramref name="value" /> ≤ <paramref name="max" />.
    ///
    /// -or-
    ///
    /// <paramref name="min" /> if <paramref name="value" /> &lt; <paramref name="min" />.
    ///
    /// -or-
    ///
    /// <paramref name="max" /> if <paramref name="max" /> &lt; <paramref name="value" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint Clamp(uint value, uint min, uint max) {
        if (min > max) {
            ThrowMinMaxException(min, max);
        }

        if (value < min) {
            return min;
        }

        return value > max ? max : value;
    }

    [DoesNotReturn]
    internal static void ThrowMinMaxException<T>(T min, T max) {
        throw new ArgumentException("The minimum value cannot be greater than the maximum value.");
    }
}
