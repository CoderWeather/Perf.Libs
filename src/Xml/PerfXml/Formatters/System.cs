using System.Globalization;

namespace PerfXml.Formatters;

public sealed class ByteFormatter : IXmlFormatter<byte> {
    public static readonly ByteFormatter Instance = new();
    ByteFormatter() { }

    public bool TryWriteTo(Span<char> span, byte value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public byte Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return byte.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}

public sealed class Int16Formatter : IXmlFormatter<short> {
    public static readonly Int16Formatter Instance = new();
    Int16Formatter() { }

    public bool TryWriteTo(Span<char> span, short value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public short Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return short.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}

public sealed class Int32Formatter : IXmlFormatter<int> {
    public static readonly Int32Formatter Instance = new();
    Int32Formatter() { }

    public bool TryWriteTo(Span<char> span, int value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public int Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return int.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}

public sealed class UInt32Formatter : IXmlFormatter<uint> {
    public static readonly UInt32Formatter Instance = new();
    UInt32Formatter() { }

    public bool TryWriteTo(Span<char> span, uint value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public uint Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return uint.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}

public sealed class Int64Formatter : IXmlFormatter<long> {
    public static readonly Int64Formatter Instance = new();
    Int64Formatter() { }

    public bool TryWriteTo(Span<char> span, long value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public long Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return long.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}

public sealed class DoubleFormatter : IXmlFormatter<double> {
    public static readonly DoubleFormatter Instance = new();
    DoubleFormatter() { }

    public bool TryWriteTo(Span<char> span, double value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public double Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return double.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}

public sealed class DecimalFormatter : IXmlFormatter<decimal> {
    public static readonly DecimalFormatter Instance = new();
    DecimalFormatter() { }

    public bool TryWriteTo(Span<char> span, decimal value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, provider: CultureInfo.InvariantCulture);
    }

    public decimal Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return decimal.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}

public sealed class StringFormatter : IXmlFormatter<string?> {
    public static readonly StringFormatter Instance = new();
    StringFormatter() { }

    public bool TryWriteTo(Span<char> span, string? value, out int charsWritten, IXmlFormatterResolver resolver) {
        if (value is null || span.Length < value.Length) {
            charsWritten = 0;
            return true;
        }

        var result = value.AsSpan().TryCopyTo(span);
        charsWritten = result ? value.Length : 0;
        return true;
    }

    public string? Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return span.Length > 0 ? span.ToString() : null;
    }
}

public sealed class CharFormatter : IXmlFormatter<char> {
    public static readonly CharFormatter Instance = new();
    CharFormatter() { }

    public bool TryWriteTo(Span<char> span, char value, out int charsWritten, IXmlFormatterResolver resolver) {
        if (span.Length < 1) {
            charsWritten = 0;
            return false;
        }

        span[0] = value;
        charsWritten = 1;
        return true;
    }

    public char Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return span[0];
    }
}

public sealed class BooleanFormatter : IXmlFormatter<bool> {
    public static readonly BooleanFormatter Instance = new();
    BooleanFormatter() { }

    public bool TryWriteTo(Span<char> span, bool value, out int charsWritten, IXmlFormatterResolver resolver) {
        if (span.Length < 1) {
            charsWritten = 0;
            return false;
        }

        span[0] = value ? '1' : '0';
        charsWritten = 1;
        return true;
    }

    public bool Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        switch (span[0]) {
        case '0': return false;
        case '1': return true;
        }

        if (span.Equals("no", StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        if (span.Equals("yes", StringComparison.InvariantCultureIgnoreCase)) {
            return true;
        }

        if (span.Equals("false", StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        if (span.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
            return true;
        }

        throw new InvalidDataException($"unknown boolean \"{span.ToString()}\"");
    }
}

public sealed class GuidFormatter : IXmlFormatter<Guid> {
    public static readonly GuidFormatter Instance = new();
    GuidFormatter() { }

    public bool TryWriteTo(Span<char> span, Guid value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten);
    }

    public Guid Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return Guid.Parse(span);
    }
}

public sealed class DateTimeFormatter : IXmlFormatter<DateTime> {
    public static readonly DateTimeFormatter Instance = new();
    readonly string format;

    public DateTimeFormatter(string format = "yyyy-MM-dd HH:mm:ss") {
        this.format = format;
    }

    public bool TryWriteTo(Span<char> span, DateTime value, out int charsWritten, IXmlFormatterResolver resolver) {
        return value.TryFormat(span, out charsWritten, format, CultureInfo.InvariantCulture);
    }

    public DateTime Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
        return DateTime.ParseExact(span, format, CultureInfo.InvariantCulture);
    }
}

#if NET6_0_OR_GREATER
sealed class DateOnlyFormatter : IXmlFormatter<DateOnly> {
    public static readonly DateOnlyFormatter Instance = new();
    readonly string format;

    public DateOnlyFormatter(string format = "yyyy-MM-dd") {
        this.format = format;
    }

    public bool TryWriteTo(Span<char> span, DateOnly value, out int charsWritten, IXmlFormatterResolver resolver) =>
        value.TryFormat(span, out charsWritten, format, CultureInfo.InvariantCulture);

    public DateOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) => DateOnly.ParseExact(span, format, CultureInfo.InvariantCulture);
}

sealed class TimeOnlyFormatter : IXmlFormatter<TimeOnly> {
    public static readonly TimeOnlyFormatter Instance = new();
    readonly string format;

    public TimeOnlyFormatter(string format = "HH:mm:ss") {
        this.format = format;
    }

    public bool TryWriteTo(Span<char> span, TimeOnly value, out int charsWritten, IXmlFormatterResolver resolver) =>
        value.TryFormat(span, out charsWritten, format, CultureInfo.InvariantCulture);

    public TimeOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) => TimeOnly.ParseExact(span, format, CultureInfo.InvariantCulture);
}
#endif
