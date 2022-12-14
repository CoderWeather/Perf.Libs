namespace PerfXml.Formatters;

public sealed class DateOnlyFormatter : IXmlFormatter<DateOnly> {
    public static readonly DateOnlyFormatter Instance = new();
    readonly string format;

    public DateOnlyFormatter(string format = "yyyy-MM-dd") {
        this.format = format;
    }

    public bool TryWriteTo(Span<char> span, DateOnly value, out int charsWritten, IXmlFormatterResolver resolver) =>
        value.TryFormat(span, out charsWritten, format);

    public DateOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver formatterResolver) => DateOnly.ParseExact(span, format);
}

public sealed class TimeOnlyFormatter : IXmlFormatter<TimeOnly> {
    public static readonly TimeOnlyFormatter Instance = new();
    readonly string format;

    public TimeOnlyFormatter(string format = "HH:mm") {
        this.format = format;
    }

    public bool TryWriteTo(Span<char> span, TimeOnly value, out int charsWritten, IXmlFormatterResolver resolver) =>
        value.TryFormat(span, out charsWritten, format);

    public TimeOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver formatterResolver) => TimeOnly.ParseExact(span, format);
}
