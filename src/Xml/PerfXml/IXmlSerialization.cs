namespace PerfXml;

/// <summary>
/// Abstract base class for types that can be read from and written to XML using <see cref="XmlReadBuffer"/> and <see cref="XmlWriteBuffer"/>
/// </summary>
public interface IXmlSerialization {
    /// <summary>Gets the name of the node to be written</summary>
    /// <returns>Name of the node to be written</returns>
    /*#if NET7_0_OR_GREATER
        public static abstract ReadOnlySpan<char> GetNodeName();
    #else
        public ReadOnlySpan<char> GetNodeName();
    #endif*/
    ReadOnlySpan<char> GetNodeName();

    bool ParseFullBody(
        ref XmlReadBuffer buffer,
        ReadOnlySpan<char> bodySpan,
        ref int end,
        IXmlFormatterResolver resolver
    );

    bool ParseSubBody(
        ref XmlReadBuffer buffer,
        ulong hash,
        ReadOnlySpan<char> bodySpan,
        ReadOnlySpan<char> innerBodySpan,
        ref int end,
        ref int endInner,
        IXmlFormatterResolver resolver
    );

    bool ParseSubBody(
        ref XmlReadBuffer buffer,
        ReadOnlySpan<char> nodeName,
        ReadOnlySpan<char> bodySpan,
        ReadOnlySpan<char> innerBodySpan,
        ref int end,
        ref int endInner,
        IXmlFormatterResolver resolver
    );

    bool ParseAttribute(
        ref XmlReadBuffer buffer,
        ulong hash,
        ReadOnlySpan<char> value,
        IXmlFormatterResolver resolver
    );

    void SerializeBody(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver);

    void SerializeAttributes(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver);

    void Serialize(
        ref XmlWriteBuffer buffer,
        IXmlFormatterResolver resolver,
        ReadOnlySpan<char> nodeName = default
    );
}
