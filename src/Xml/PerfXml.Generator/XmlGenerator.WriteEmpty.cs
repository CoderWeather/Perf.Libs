namespace PerfXml.Generator;

partial class XmlGenerator {
    static void WriteEmptyParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end, IXmlFormatterResolver resolver) => default;"
        );
    }

    static void WriteEmptyParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver) => default;"
        );
    }

    static void WriteEmptyParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
        writer.WriteLine(
            $"{cls.AdditionalInheritanceMethodModifiers}bool IXmlSerialization.ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver) => default;"
        );
    }
}
