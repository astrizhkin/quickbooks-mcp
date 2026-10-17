using System.Text.Json;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class InventoryToolsTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    [Fact]
    public async Task Sites_are_searched_as_a_list()
    {
        var json = await new InventoryTools(_qb).SearchInventorySites("Main");

        Assert.Equal("InventorySiteQueryRq", _qb.SentRequest());
        Assert.Contains("Example InventorySite", json);
    }

    [Theory]
    [InlineData("InventoryAdjustment")]
    [InlineData("TransferInventory")]
    [InlineData("BuildAssembly")]
    public async Task Each_inventory_transaction_type_is_queried(string type)
    {
        var json = await new InventoryTools(_qb).SearchInventoryTransactions(type, dateFrom: "2025-01-01", includeLineItems: true);

        Assert.Equal(type + "QueryRq", _qb.SentRequest());
        Assert.Contains("4D-1000000001", json);
    }

    [Fact]
    public async Task Other_transaction_types_are_refused() =>
        await ToolTestHelpers.RejectsAsync(() => new InventoryTools(_qb).SearchInventoryTransactions("Invoice"));

    [Fact]
    public async Task Item_sites_by_type_or_by_item_and_site()
    {
        var tools = new InventoryTools(_qb);

        await tools.GetItemSites(itemType: "inventory");
        await tools.GetItemSites(item: "Widget", site: "Main");

        Assert.Equal(["ItemTypeFilter", "MaxReturned", "ActiveStatus"], _qb.Requests[0].Root!.Element("QBXMLMsgsRq")!.Elements().First().Elements().Select(e => e.Name.LocalName));
        Assert.Equal(["ItemSiteFilter", "MaxReturned", "ActiveStatus"], _qb.Requests[1].Root!.Element("QBXMLMsgsRq")!.Elements().First().Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public async Task Item_sites_type_cannot_be_combined_with_item() =>
        await ToolTestHelpers.RejectsAsync(() => new InventoryTools(_qb).GetItemSites(itemType: "Inventory", item: "Widget"));

    [Fact]
    public async Task Assemblies_can_build_sends_the_assembly_and_date()
    {
        var json = await new InventoryTools(_qb).GetAssembliesCanBuild("Gift Basket", "2025-03-01");

        Assert.Equal("ItemAssembliesCanBuildQueryRq", _qb.SentRequest());
        Assert.Contains("QuantityOnHand", json);
    }

    [Fact]
    public async Task Inventory_adjustments_and_sites_can_be_created_with_the_write_tools()
    {
        var executor = new WriteExecutor(_qb, new WriteGate(WriteOp.All), new AuditLog(_log));
        var tools = new InventoryWriteTools(new RecordWriter(_qb, executor));

        var site = await tools.Create("InventorySite", JsonDocument.Parse("""{ "Name": "Main Warehouse" }""").RootElement);
        var adjustment = await tools.Create("InventoryAdjustment", JsonDocument.Parse("""
            { "AccountRef": { "FullName": "Inventory Adjustments" },
              "InventoryAdjustmentLineAdd": [ { "ItemRef": { "FullName": "Widget" }, "QuantityAdjustment": { "NewQuantity": 10 } } ] }
            """).RootElement);

        Assert.Contains("InventorySite", site);
        Assert.Contains("4D-2000000001", adjustment);
        Assert.Equal(["InventorySiteAddRq", "InventoryAdjustmentAddRq"], _qb.Requests.Select(r => r.Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName));
    }

    [Fact]
    public async Task Inventory_write_tools_refuse_other_entities() =>
        await Assert.ThrowsAsync<McpException>(() => new InventoryWriteTools(new RecordWriter(_qb,
            new WriteExecutor(_qb, new WriteGate(WriteOp.All), new AuditLog(_log)))).Create("Invoice", JsonDocument.Parse("{}").RootElement));
}
