// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Perf.Holders.Generator.Internal;

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

static class CollectionExtensions {
    public static ImmutableArray<T> WhereOfType<T>(this ImmutableArray<ISymbol> symbols, Func<T, bool> predicate)
        where T : ISymbol {
        var count = 0;
        var span = symbols.AsSpan();
        foreach (ref readonly var sr in span) {
            if (sr is T ps && predicate(ps)) {
                count++;
            }
        }

        if (count is 0) {
            return ImmutableArray<T>.Empty;
        }

        var results = new T[count];
        var i = 0;
        foreach (ref readonly var sr in span) {
            if (sr is T ps) {
                results[i++] = ps;
            }
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(results);
    }

    public static T[] GetUnderlyingArray<T>(this List<T> list) => ListArrayAccessor<T>.Func(list);

    static class ListArrayAccessor<T> {
        public static readonly Func<List<T>, T[]> Func;

        static ListArrayAccessor() {
            var list = typeof(List<T>);
            var field = list.GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var p1 = Expression.Parameter(typeof(List<T>), "list");
            var lambda = Expression.Lambda<Func<List<T>, T[]>>(
                Expression.Field(p1, field),
                p1
            );
            Func = lambda.Compile();
        }
    }
}

static class StringExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> span) {
        if (span.Length is 0) {
            return sb;
        }

        unsafe {
            var p = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
            sb.Append(p, span.Length);
        }

        return sb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToFieldFormat(this string s) {
        if (string.IsNullOrWhiteSpace(s)) {
            return s;
        }

        if (s!.Length is 1) {
            return char.IsLower(s[0]) ? $"_{s[0]}" : char.ToLower(s[0]).ToString();
        }

        var addUnderscore = char.IsLower(s[0]);

        Span<char> buffer = stackalloc char[addUnderscore ? s.Length + 1 : s.Length];
        var i = 0;
        if (addUnderscore) {
            buffer[i++] = '_';
        }

        buffer[i++] = char.ToLower(s[0]);
        s.AsSpan()[1..].CopyTo(buffer[i..]);

        string result;
        unsafe {
            var p = (char*)Unsafe.AsPointer(ref buffer.GetPinnableReference());
            result = new(p, 0, buffer.Length);
        }

        return result;
    }

    public static int IndexOfBeforeNewLine(this ReadOnlySpan<char> span, char value) {
        if (span.Length is 0) {
            return -1;
        }

        var i = 0;
        var length = span.Length;
        while (i < length) {
            if (length - i >= 4) {
                var c0 = span[i];
                if (c0 == value) {
                    return i;
                }

                if (c0 == '\n') {
                    return -1;
                }

                var c1 = span[i + 1];
                if (c1 == value) {
                    return i + 1;
                }

                if (c1 == '\n') {
                    return -1;
                }

                var c2 = span[i + 2];
                if (c2 == value) {
                    return i + 2;
                }

                if (c2 == '\n') {
                    return -1;
                }

                var c3 = span[i + 3];
                if (c3 == value) {
                    return i + 3;
                }

                if (c3 == '\n') {
                    return -1;
                }

                i += 4;
            } else {
                var c = span[i];
                if (c == value) {
                    return i;
                }

                if (c is '\n') {
                    return -1;
                }

                i++;
            }
        }

        return -1;
    }

    public static bool IsEntryWord(this ReadOnlySpan<char> span) {
        if (char.IsLetter(span[0]) is false) {
            return false;
        }

        if (span.Length is 0 or > 50) {
            return false;
        }

        span = span[1..];
        foreach (var c in span) {
            if (c is
                not (>= '0' and <= '9')
            and not (>= 'a' and <= 'z')
            and not (>= 'A' and <= 'Z')
            and not ('_' or '.' or '-' or ' ' or '/')
            ) {
                return false;
            }
        }

        return true;
    }
}

static class StringBuilderExtensions {
    public static StringBuilder AppendInterpolated(this StringBuilder sb, DefaultInterpolatedStringHandler interpolatedStringHandler) {
        sb.Append(interpolatedStringHandler.Text);
        interpolatedStringHandler.Clear();
        return sb;
    }

    public static StringBuilder AppendInterpolatedLine(this StringBuilder sb, DefaultInterpolatedStringHandler interpolatedStringHandler) {
        AppendInterpolated(sb, interpolatedStringHandler);
        sb.AppendLine();
        return sb;
    }
}
