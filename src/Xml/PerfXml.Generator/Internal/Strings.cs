namespace PerfXml.Generator.Internal;

static class Strings {
    public static unsafe string ToLowerCamelCase(this string str) {
        if (string.IsNullOrEmpty(str) || char.IsUpper(str[0]) is false) {
            return str;
        }

        var span = str.AsSpan();
        Span<char> newString = stackalloc char[str.Length];
        newString[0] = char.ToLowerInvariant(span[0]);
        span.Slice(1).CopyTo(newString.Slice(1));

        string result;
        fixed (char* ptr = newString) {
            result = new(ptr, 0, newString.Length);
        }

        return result;
    }
}
