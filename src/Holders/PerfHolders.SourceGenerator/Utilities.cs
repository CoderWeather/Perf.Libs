using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Perf.Holders.Generator;

using System.Text;

static class PatternFormatter {
    public static string Format(string pattern, Dictionary<string, string?> values) {
        var sb = new StringBuilder();
        var span = pattern.AsSpan();

        while (span.Length > 0) {
            var nextIndex = span.IndexOf('{');
            if (nextIndex is -1) {
                sb.Append(span);
                break;
            }

            sb.Append(span[..nextIndex]);

            var entryStart = span[(nextIndex + 1)..];
            var found = false;
            foreach (var p in values) {
                if (entryStart.StartsWith(p.Key.AsSpan())) {
                    var k = p.Key;
                    var v = p.Value;
                    sb.Append(v);
                    span = entryStart[(k.Length + 1)..];
                    found = true;
                    break;
                }
            }

            if (found is false) {
                sb.Append(span[nextIndex]);
                span = span[(nextIndex + 1)..];
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

    static bool IsEntryWord(this ReadOnlySpan<char> span) {
        if (char.IsLetter(span[0]) is false) {
            return false;
        }

        if (span.Length is 0 or > 30) {
            return false;
        }

        span = span[1..];
        foreach (var c in span) {
            if (char.IsLetter(c) is false || char.IsDigit(c) is false) {
                return false;
            }
        }

        return true;
    }
}
