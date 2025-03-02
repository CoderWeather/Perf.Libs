namespace Perf.Holders.Generator.Internal;

using System.Runtime.CompilerServices;
using System.Text;

static class CollectionExtensions {
    //
}

static class StringExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> span) {
        if (span.Length is 0) {
            return sb;
        }

        ref readonly var spanRef = ref span.GetPinnableReference();
        unsafe {
            var p = (char*)Unsafe.AsPointer(ref Unsafe.AsRef(in spanRef));
            sb.Append(p, span.Length);
        }

        return sb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? ToLowerFirstChar(this string? s) {
        if (string.IsNullOrWhiteSpace(s)) {
            return s;
        }

        if (s!.Length is 1) {
            return char.ToLower(s[0]).ToString();
        }

        Span<char> buffer = stackalloc char[s.Length];
        buffer[0] = char.ToLower(s[0]);
        s.AsSpan()[1..].CopyTo(buffer[1..]);

        string result;
        unsafe {
            var p = (char*)Unsafe.AsPointer(ref buffer.GetPinnableReference());
            result = new string(p, 0, s.Length);
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
