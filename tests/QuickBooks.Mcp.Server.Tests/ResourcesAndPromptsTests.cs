using QuickBooks.Mcp.Server.Prompts;
using QuickBooks.Mcp.Server.Resources;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class ResourcesAndPromptsTests
{
    [Fact]
    public async Task Accounts_resource_lists_accounts()
    {
        var qb = new FixtureQbClient();

        var json = await new ListResources(qb).Accounts(default);

        Assert.Equal("AccountQueryRq", qb.SentRequest());
        Assert.Contains("Checking", json);
    }

    [Fact]
    public async Task Items_resource_lists_items() =>
        Assert.Contains("Example", await new ListResources(new FixtureQbClient()).Items(default));

    [Fact]
    public void Prompts_mention_their_arguments()
    {
        Assert.Contains("2025-03", WorkflowPrompts.MonthEndReview("2025-03"));
        Assert.Contains("45 days", WorkflowPrompts.ArCollectionsFollowUp(45));
        Assert.Contains("vendors", WorkflowPrompts.FindDuplicateVendors());
    }
}
