using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class NameToolsTests
{
    [Theory]
    [InlineData("customers", "CustomerQueryRq")]
    [InlineData("vendors", "VendorQueryRq")]
    [InlineData("employees", "EmployeeQueryRq")]
    [InlineData("other", "OtherNameQueryRq")]
    [InlineData("names", "EntityQueryRq")]
    public async Task Each_tool_queries_its_entity(string tool, string request)
    {
        var qb = new FixtureQbClient();
        var tools = new NameTools(qb);

        var json = await (tool switch
        {
            "customers" => tools.SearchCustomers(nameContains: "Ex"),
            "vendors" => tools.SearchVendors(nameContains: "Ex"),
            "employees" => tools.SearchEmployees(nameContains: "Ex"),
            "other" => tools.SearchOtherNames(nameContains: "Ex"),
            _ => tools.SearchNames(nameContains: "Ex"),
        });

        Assert.Equal(request, qb.SentRequest());
        Assert.Contains("Example", json);
    }

    [Fact]
    public async Task Other_names_can_search_leads()
    {
        var qb = new FixtureQbClient();

        await new NameTools(qb).SearchOtherNames("lead");

        Assert.Equal("LeadQueryRq", qb.SentRequest());
    }

    [Fact]
    public async Task Other_names_rejects_unknown_kind() =>
        await ToolTestHelpers.RejectsAsync(() => new NameTools(new FixtureQbClient()).SearchOtherNames("Invoice"));
}
