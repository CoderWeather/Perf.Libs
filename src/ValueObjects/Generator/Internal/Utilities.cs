namespace Perf.ValueObjects.Generator.Internal;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

static class PatternFormatter {
    public static string Format(string pattern, Dictionary<string, string?> values) {
        var sb = new StringBuilder();
        var span = pattern.AsSpan();

        while (span.Length > 0) {
            var nextEntryIndex = span.IndexOf('{');
            if (nextEntryIndex is -1) {
                sb.Append(span);
                break;
            }

            sb.Append(span[..nextEntryIndex]);

            var cAfter = span[nextEntryIndex + 1];
            if (cAfter is '{' or '\n' or ' ' || char.IsLetter(cAfter) is false) {
                sb.Append('{');
                span = span[(nextEntryIndex + 1)..];
                continue;
            }

            var entrySpan = span[(nextEntryIndex + 1)..];
            var endEntryIndex = entrySpan.IndexOfBeforeNewLine('}');
            if (endEntryIndex is -1) {
                sb.Append('{');
                span = span[(nextEntryIndex + 1)..];
                continue;
            }

            entrySpan = entrySpan[..endEntryIndex];

            if (entrySpan.IsEntryWord() is false) {
                sb.Append('{');
                span = span[(nextEntryIndex + 1)..];
                continue;
            }

            var found = false;
            foreach (var p in values) {
                var k = p.Key;
                var v = p.Value;
                if (entrySpan.SequenceEqual(k.AsSpan())) {
                    sb.Append(v);
                    span = span[(nextEntryIndex + endEntryIndex + 2)..];
                    // span = entryStart[(k.Length + 1)..];
                    found = true;
                    break;
                }
            }

            if (found is false) {
                sb.Append(span[nextEntryIndex]);
                span = span[(nextEntryIndex + 1)..];
            }
        }

        return sb.ToString();
    }

    static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> span) {
        if (span.Length is 0) {
            return sb;
        }

        ref var spanRef = ref MemoryMarshal.GetReference(span);
        unsafe {
            var p = (char*)Unsafe.AsPointer(ref spanRef);
            sb.Append(p, span.Length);
        }

        return sb;
    }

    static int IndexOfBeforeNewLine(this ReadOnlySpan<char> span, char value) {
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

    static bool IsEntryWord(this ReadOnlySpan<char> span) {
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
