using System.Xml.Linq;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Core.Tests.QbXml;

public class QbXmlJsonTests
{
    [Fact]
    public void Repeated_elements_become_arrays_and_leaves_strings()
    {
        var record = XElement.Parse("<Ret><Name>A</Name><Line><Qty>1</Qty></Line><Line><Qty>2</Qty></Line></Ret>");

        var json = QbXmlJson.ToNode(record);

        Assert.Equal("A", (string?)json["Name"]);
        Assert.Equal(2, json["Line"]!.AsArray().Count);
        Assert.Equal("2", (string?)json["Line"]![1]!["Qty"]);
    }
}
