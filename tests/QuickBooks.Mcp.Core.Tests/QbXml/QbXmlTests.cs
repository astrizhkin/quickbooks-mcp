using System.Xml.Linq;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Core.Tests.QbXml;

public class QbXmlTests
{
    [Fact]
    public void PickVersion_takes_highest_supported()
    {
        Assert.Equal("16.0", QbXmlRequest.PickVersion(["13.0", "16.0", "99.0", "2.1"]));
        Assert.Equal("2.1", QbXmlRequest.PickVersion(["2.1", "99.0"]));
    }

    [Fact]
    public void PickVersion_throws_when_none_supported() =>
        Assert.Throws<InvalidOperationException>(() => QbXmlRequest.PickVersion(["99.0"]));

    [Fact]
    public void Build_creates_envelope_with_iterator()
    {
        var xml = QbXmlRequest.Build("16.0", "CustomerQueryRq",
            [new XElement("MaxReturned", 50)], iterator: "Start");

        Assert.Contains("<?qbxml version=\"16.0\"?>", xml);
        var rq = XDocument.Parse(xml).Root!.Element("QBXMLMsgsRq")!;
        Assert.Equal("stopOnError", (string?)rq.Attribute("onError"));
        var req = rq.Element("CustomerQueryRq")!;
        Assert.Equal("Start", (string?)req.Attribute("iterator"));
        Assert.Equal("50", (string?)req.Element("MaxReturned"));
    }

    [Fact]
    public void Parse_reads_records_and_iterator()
    {
        const string xml = """
            <?xml version="1.0"?><QBXML><QBXMLMsgsRs>
            <CustomerQueryRs requestID="1" statusCode="0" statusSeverity="Info" statusMessage="Status OK" iteratorID="{abc}" iteratorRemainingCount="7">
              <CustomerRet><ListID>1</ListID><Name>Acme</Name></CustomerRet>
            </CustomerQueryRs></QBXMLMsgsRs></QBXML>
            """;
        var r = QbXmlResponse.Parse(xml);

        Assert.Equal("Acme", (string?)r.Records.Single().Element("Name"));
        Assert.Equal("{abc}", r.IteratorId);
        Assert.Equal(7, r.IteratorRemainingCount);
    }

    [Fact]
    public void Parse_no_match_is_empty()
    {
        const string xml = """<QBXML><QBXMLMsgsRs><CustomerQueryRs statusCode="1" statusSeverity="Info" statusMessage="No match"/></QBXMLMsgsRs></QBXML>""";
        Assert.Empty(QbXmlResponse.Parse(xml).Records);
    }

    [Fact]
    public void Parse_error_throws_typed_exception()
    {
        const string xml = """<QBXML><QBXMLMsgsRs><CustomerAddRs statusCode="3100" statusSeverity="Error" statusMessage="Name already in use"/></QBXMLMsgsRs></QBXML>""";
        var ex = Assert.Throws<QbException>(() => QbXmlResponse.Parse(xml));
        Assert.Equal(3100, ex.StatusCode);
    }

    [Theory]
    [InlineData("DataEventSubscriptionQueryRq")]
    [InlineData("UIEventSubscriptionQueryRq")]
    [InlineData("UIExtensionSubscriptionQueryRq")]
    [InlineData("SubscriptionDelRq")]
    public void Subscription_requests_use_the_subscription_envelope_without_onError(string request)
    {
        var root = XDocument.Parse(QbXmlRequest.Build("16.0", request, [new XElement("SubscriberID", "{00000000-0000-0000-0000-000000000000}")])).Root!;

        Assert.Null(root.Element("QBXMLMsgsRq"));
        var envelope = root.Element(QbXmlRequest.SubscriptionEnvelope)!;
        Assert.Null(envelope.Attribute("onError"));
        Assert.NotNull(envelope.Element(request));
    }

    [Fact]
    public void Data_event_recovery_info_is_an_ordinary_request() =>
        Assert.NotNull(XDocument.Parse(QbXmlRequest.Build("16.0", "DataEventRecoveryInfoQueryRq")).Root!.Element("QBXMLMsgsRq"));

    [Fact]
    public void Parse_reads_a_subscription_reply_and_treats_no_match_as_empty()
    {
        const string xml = """<QBXML><QBXMLSubscriptionMsgsRs><DataEventSubscriptionQueryRs statusCode="1" statusSeverity="Info" statusMessage="A query request did not find a matching object in QuickBooks" /></QBXMLSubscriptionMsgsRs></QBXML>""";

        var response = QbXmlResponse.Parse(xml);

        Assert.Equal(1, response.StatusCode);
        Assert.Empty(response.Records);
    }
}
