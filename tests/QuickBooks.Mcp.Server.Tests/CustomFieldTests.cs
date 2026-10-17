using System.Xml.Linq;
using ModelContextProtocol;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class CustomFieldTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    private CustomFieldWriteTools Writes(IQbClient? client = null) =>
        new(new WriteExecutor(client ?? _qb, new WriteGate(WriteOp.All), new AuditLog(_log)));

    [Fact]
    public async Task Definitions_and_sales_tax_payable_are_read()
    {
        var tools = new CustomFieldTools(_qb);

        Assert.Contains("Region", await tools.GetCustomFieldDefinitions());
        Assert.Contains("125.00", await tools.GetSalesTaxPayable("2025-03-31"));
        await tools.GetCustomFieldDefinitions("Customer");

        Assert.Equal(["DataExtDefQueryRq", "SalesTaxPayableQueryRq", "DataExtDefQueryRq"],
            _qb.Requests.Select(r => r.Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName));
    }

    [Fact]
    public async Task Queries_ask_for_custom_fields_with_owner_id_zero()
    {
        await new QueryTools(_qb).QueryRecords("Customer", includeCustomFields: true);
        await new NameTools(_qb).SearchVendors(includeCustomFields: true);

        Assert.All(_qb.Requests, r => Assert.Equal("0", (string?)r.Descendants("OwnerID").Single()));
    }

    [Fact]
    public async Task Entities_without_custom_fields_say_so() =>
        await ToolTestHelpers.RejectsAsync(() => new QueryTools(_qb).QueryRecords("Class", includeCustomFields: true));

    [Fact]
    public async Task A_definition_is_created_with_its_record_types()
    {
        await Writes().CreateDefinition("Region", "str255type", ["Customer", "Invoice"]);

        var add = _qb.Requests[0].Descendants("DataExtDefAdd").Single();
        Assert.Equal(["OwnerID", "DataExtName", "DataExtType", "AssignToObject", "AssignToObject"], add.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("STR255TYPE", (string?)add.Element("DataExtType"));
    }

    [Fact]
    public async Task Bad_definitions_are_refused()
    {
        await ToolTestHelpers.RejectsAsync(() => Writes().CreateDefinition("Region", "TEXT", ["Customer"]));
        await ToolTestHelpers.RejectsAsync(() => Writes().CreateDefinition("Region", "STR255TYPE", []));
    }

    [Theory]
    [InlineData("Customer", "ListDataExtType", "Customer")]
    [InlineData("ItemService", "ListDataExtType", "Item")]
    [InlineData("Invoice", "TxnDataExtType", "Invoice")]
    public async Task Values_address_lists_by_list_id_and_transactions_by_txn_id(string recordType, string typeElement, string kind)
    {
        await Writes().UpdateValue(recordType, "ID-1", "Region", "West");

        var add = _qb.Requests[0].Descendants("DataExtAdd").Single();
        Assert.Equal(kind, (string?)add.Element(typeElement));
        Assert.Equal("West", (string?)add.Element("DataExtValue"));
    }

    private sealed class AddFailsClient : IQbClient
    {
        public List<string> Sent { get; } = [];

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            var request = XDocument.Parse(buildRequest("16.0")).Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName;
            Sent.Add(request);
            return Task.FromResult(request == "DataExtAddRq"
                ? """<QBXML><QBXMLMsgsRs><DataExtAddRs statusCode="3200" statusSeverity="Error" statusMessage="exists"/></QBXMLMsgsRs></QBXML>"""
                : """<QBXML><QBXMLMsgsRs><DataExtModRs statusCode="0"><DataExtRet><DataExtName>Region</DataExtName></DataExtRet></DataExtModRs></QBXMLMsgsRs></QBXML>""");
        }
    }

    [Fact]
    public async Task An_existing_value_is_changed_instead_of_added()
    {
        var client = new AddFailsClient();

        var json = await Writes(client).UpdateValue("Customer", "ID-1", "Region", "East");

        Assert.Equal(["DataExtAddRq", "DataExtModRq"], client.Sent);
        Assert.Contains("Region", json);
    }

    [Fact]
    public async Task Deleting_a_definition_or_value_needs_a_confirmation_token()
    {
        var tools = Writes();

        Assert.Contains("confirmation_required", await tools.DeleteDefinition("Region"));
        Assert.Contains("confirmation_required", await tools.DeleteValue("Customer", "ID-1", "Region"));
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Confirmed_deletes_are_sent()
    {
        var tools = Writes();
        var token = System.Text.Json.JsonDocument.Parse(await tools.DeleteDefinition("Region")).RootElement.GetProperty("confirmation_token").GetString();

        await tools.DeleteDefinition("Region", token);

        Assert.Equal("DataExtDefDelRq", _qb.SentRequest());
    }

    [Fact]
    public async Task Unknown_record_types_are_refused() =>
        await Assert.ThrowsAsync<McpException>(() => Writes().UpdateValue("Bogus", "1", "Region", "x"));
}

public sealed class CustomFieldValueDeleteTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");

    public void Dispose() => File.Delete(_log);

    [Fact]
    public async Task A_confirmed_value_delete_is_sent_and_matches_the_schema()
    {
        var qb = new FixtureQbClient();
        var tools = new CustomFieldWriteTools(new WriteExecutor(qb, new WriteGate(WriteOp.All), new AuditLog(_log)));
        var token = System.Text.Json.JsonDocument.Parse(await tools.DeleteValue("Customer", "ID-1", "Region")).RootElement.GetProperty("confirmation_token").GetString();

        await tools.DeleteValue("Customer", "ID-1", "Region", token);

        Assert.Equal("DataExtDelRq", qb.SentRequest());
    }
}
