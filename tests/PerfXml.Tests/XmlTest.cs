using System.Text.Json;

namespace PerfXml.Tests;

using CommunityToolkit.HighPerformance.Buffers;
using Xunit.Abstractions;

public class XmlTest {
    readonly ITestOutputHelper testOutputHelper;

    public XmlTest(ITestOutputHelper testOutputHelper) {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Work() {
        // const string xml = "<GPRS><Node>2</Node><PDP>10.34.161.129</PDP><QoSUse><rlblt>1</rlblt></QoSUse></GPRS>";
        var xml = """
<?xml version="1.0" encoding="UTF-8"?>
<GPRS Test="1234">
    <Node>2</Node>
    <PDP>10.34.161.129</PDP>
    <QoSUse>
        <rlblt>1</rlblt>
    </QoSUse>
</GPRS>
""";
        var model = Xml.Deserialize<GrpsModel>(xml);
        var serialized = Xml.Serialize(model);
        testOutputHelper.WriteLine(
            JsonSerializer.Serialize(
                model,
                new JsonSerializerOptions() {
                    WriteIndented = true
                }
            )
        );
    }

    static ReadOnlySpan<char> TestFoo<T>(T _) where T : IXmlSerialization => T.GetNodeName();

    [Fact]
    public void Sbrf() {
        var response = new TestResult() { Result = 0, Comment = null };
        using var so = SpanOwner<char>.Allocate(1024);
        // var xml = Xml.Serialize(response).ToString();
        // _ = xml;
        Xml.Serialize(response, so.Span, out var written);
        var xml = so.Span.Slice(0, written).ToString();
    }
}

[XmlCls("response")]
sealed partial class TestResult : IXmlSerialization {
    [XmlBody("result")] public int Result { get; set; }
    [XmlBody("comment")] public string? Comment { get; set; }
}
/*
 <GPRS>
 	<Node>2</Node>
 	<PDP>10.34.161.129</PDP>
 	<sgsn>185.77.18.3</sgsn>
 	<ggsn>185.77.18.2</ggsn>
 	<chrgID>158949122</chrgID>
 	<duration>280</duration>
 	<APN>em.ru</APN>
 	<VolIn>2261446</VolIn>
 	<VolOut>496968</VolOut>
 	<QoSUse>
 		<rlblt>1</rlblt>
 	</QoSUse>
 </GPRS>
 */

[XmlCls("GRPS")]
sealed partial class GrpsModel : IXmlSerialization {
    [XmlField("Test")]
    public string? TestStringAttribute { get; set; }

    [XmlBody("Node")]
    public int Node { get; set; }

    [XmlBody("PDP")]
    public string Pdp { get; set; } = default!;

    // [XmlBody("qqq")]
    [XmlBody(true)]
    public QoSuse QoSuse { get; set; } = default!;
}

[XmlCls("QoSUse")]
sealed partial class QoSuse : IXmlSerialization {
    [XmlBody("rlblt")]
    public int Rlblt { get; set; }

    [XmlField("field")]
    public string? TestString { get; set; }
}

public static class Tst {
    public static void Foo() {
        var c = new Cls1();
        // c.Foo();
    }
}

file static class Ext {
    public static void Foo<T>(this T c) where T : ICls => c.Foo();
}

interface ICls {
    void Foo();
}

sealed class Cls1 : ICls {
    void ICls.Foo() {
        throw new NotImplementedException();
    }
}
