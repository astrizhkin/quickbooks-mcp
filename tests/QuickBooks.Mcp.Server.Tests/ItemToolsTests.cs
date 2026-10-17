using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class ItemToolsTests
{
    [Theory]
    [InlineData(null, "ItemQueryRq")]
    [InlineData("Service", "ItemServiceQueryRq")]
    [InlineData("inventory", "ItemInventoryQueryRq")]
    [InlineData("ItemInventoryAssembly", "ItemInventoryAssemblyQueryRq")]
    public async Task Item_type_selects_the_request(string? type, string request)
    {
        var qb = new FixtureQbClient();

        var json = await new ItemTools(qb).SearchItems(type);

        Assert.Equal(request, qb.SentRequest());
        Assert.Contains("Example", json);
    }

    [Fact]
    public async Task Unknown_type_is_rejected() =>
        await ToolTestHelpers.RejectsAsync(() => new ItemTools(new FixtureQbClient()).SearchItems("Vendor"));
}
