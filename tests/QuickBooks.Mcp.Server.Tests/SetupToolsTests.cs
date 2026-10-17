using QuickBooks.Mcp.TestSupport;
using QuickBooks.Mcp.Server.Tools;

namespace QuickBooks.Mcp.Server.Tests;

public class SetupToolsTests
{
    [Fact]
    public async Task Search_accounts_queries_accounts()
    {
        var qb = new FixtureQbClient();

        var json = await new SetupTools(qb).SearchAccounts(nameContains: "Check");

        Assert.Equal("AccountQueryRq", qb.SentRequest());
        Assert.Contains("NameFilter", qb.SentElements());
        Assert.Contains("Checking", json);
    }

    [Fact]
    public async Task Search_setup_lists_maps_list_to_request()
    {
        var qb = new FixtureQbClient();

        await new SetupTools(qb).SearchSetupLists("class");

        Assert.Equal("ClassQueryRq", qb.SentRequest());
    }

    [Fact]
    public async Task Search_setup_lists_rejects_other_entities() =>
        await ToolTestHelpers.RejectsAsync(() => new SetupTools(new FixtureQbClient()).SearchSetupLists("Invoice"));

    [Fact]
    public async Task Get_preferences_returns_records() =>
        Assert.Contains("IsUsingAccountNumbers", await new SetupTools(new FixtureQbClient()).GetPreferences(default));
}
