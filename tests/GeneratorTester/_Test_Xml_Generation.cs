namespace GeneratorTester;

using PerfXml;
using System.Collections.Generic;

[XmlCls("Модель")]
sealed partial class Model : IXmlSerialization {
    [XmlBody("ID")]
    public int? Id { get; set; }

    [XmlBody("INNERS")]
    public List<InnerModel> Inners { get; set; } = new();
}

[XmlCls("INNER")]
sealed partial class InnerModel : IXmlSerialization {
    [XmlBody("NAME")]
    public string Name { get; set; } = "";
}


