namespace Perf.Holders.Generator.Internal;

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
}
