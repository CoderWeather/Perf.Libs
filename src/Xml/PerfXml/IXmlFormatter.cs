namespace PerfXml;

public interface IXmlFormatter {
    internal Type Type() {
        throw new InvalidOperationException();
    }
}

public interface IXmlFormatter<T> : IXmlFormatter {
#region IXmlFormatter Members

    Type IXmlFormatter.Type() {
        return typeof(T);
    }

#endregion

    bool TryWriteTo(Span<char> span, T value, out int charsWritten, IXmlFormatterResolver resolver);

    T Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver);
}

public interface IXmlFormatterResolver {
    IXmlFormatter<T>? GetFormatter<T>();

    internal IXmlFormatter<T> GetRequiredFormatter<T>() {
        return GetFormatter<T>() ?? throw new($"No registered formatter for {typeof(T)}");
    }
}
