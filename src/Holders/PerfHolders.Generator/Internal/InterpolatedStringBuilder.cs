namespace Perf.Holders.Generator.Internal;

using System.Runtime.CompilerServices;
using System.Text;

sealed class InterpolatedStringBuilder(
    string indentTag = "    ",
    StringBuilder? stringBuilder = null
) {
    readonly StringBuilder sb = stringBuilder ?? new();

    int indent;
    public int Indent { get => indent; set => indent = Math.Max(0, value); }

    bool shouldIndent;

    void ValidateIndent() {
        if (shouldIndent || sb is [ .., '\n' ]) {
            for (var i = 0; i < indent; i++) {
                sb.Append(indentTag);
            }

            shouldIndent = false;
            return;
        }

        var targetIndentLength = indentTag.Length * indent;
        var currentIndentLength = 0;
        for (var i = sb.Length - 1; i >= 0; i--) {
            var ch = sb[i];
            if (ch is '\n') {
                break;
            }

            if (ch is ' ') {
                currentIndentLength++;
            } else {
                shouldIndent = false;
                return;
            }
        }

        shouldIndent = false;
        if (currentIndentLength >= targetIndentLength) {
            return;
        }

        for (var i = 0; i < targetIndentLength - currentIndentLength; i++) {
            sb.Append(' ');
        }
    }

    public void Append(char c) {
        ValidateIndent();
        sb.Append(c);
        shouldIndent = false;
    }

    public void Append(string s) {
        ValidateIndent();
        sb.Append(s);
        shouldIndent = false;
    }

    public void AppendLine(string s) {
        ValidateIndent();
        sb.AppendLine(s);
        shouldIndent = true;
    }

    public void AppendLine() {
        sb.AppendLine();
        shouldIndent = true;
    }

    public void AppendInterpolated(DefaultInterpolatedStringHandler interpolatedString) {
        ValidateIndent();

        var span = interpolatedString.Text;
        while (span.Length > 0) {
            var newLineIndex = span.IndexOf('\n');
            if (newLineIndex is -1) {
                sb.Append(span);
                break;
            }

            sb.Append(span[..newLineIndex]);
            sb.AppendLine();
            shouldIndent = true;
            ValidateIndent();
            span = span[(newLineIndex + 1)..];
        }

        interpolatedString.Clear();
    }

    public void AppendInterpolatedLine(DefaultInterpolatedStringHandler interpolatedString) {
        ValidateIndent();
        AppendInterpolated(interpolatedString);
        sb.AppendLine();
    }

    public override string ToString() => sb.ToString();
    public void Clear() => sb.Clear();
}
