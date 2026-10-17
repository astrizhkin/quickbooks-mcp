using System.Text.Json;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class ChangeToolsTests
{
    [Fact]
    public async Task Changes_cover_lists_and_transactions_and_return_the_next_sync_time()
    {
        var qb = new FixtureQbClient();

        var json = JsonDocument.Parse(await new ChangeTools(qb).GetChangesSince("2025-03-01T08:30:00", ["Customer", "Invoice"])).RootElement;

        var changes = json.GetProperty("changes").EnumerateArray().ToList();
        Assert.Equal(["Customer", "Invoice"], changes.Select(c => c.GetProperty("entity").GetString()));
        Assert.All(changes, c => Assert.Equal(1, c.GetProperty("count").GetInt32()));
        Assert.True(DateTime.TryParse(json.GetProperty("next_since").GetString(), out _));
        Assert.Equal("2025-03-01T08:30:00", (string?)_Requests(qb)[0].Descendants("FromModifiedDate").Single());
        Assert.Equal("2025-03-01T08:30:00", (string?)_Requests(qb)[1].Descendants("FromModifiedDate").Single());
    }

    private static List<System.Xml.Linq.XDocument> _Requests(FixtureQbClient qb) => qb.Requests;

    [Theory]
    [InlineData("list", "ListDeletedQueryRq", "Gone Co")]
    [InlineData("transaction", "TxnDeletedQueryRq", "1009")]
    public async Task Deleted_records_are_queried_by_kind(string kind, string request, string expected)
    {
        var qb = new FixtureQbClient();

        var json = await new ChangeTools(qb).GetDeletedRecords(kind, "2025-03-01");

        Assert.Equal(request, qb.SentRequest());
        Assert.Contains(expected, json);
    }

    [Fact]
    public async Task Deleted_types_are_validated_and_can_be_limited()
    {
        var qb = new FixtureQbClient();
        var tools = new ChangeTools(qb);

        await tools.GetDeletedRecords("list", "2025-03-01", ["customer"]);

        Assert.Equal(["Customer"], qb.Requests[0].Descendants("ListDelType").Select(e => e.Value));
        await ToolTestHelpers.RejectsAsync(() => tools.GetDeletedRecords("list", "2025-03-01", ["Invoice"]));
        await ToolTestHelpers.RejectsAsync(() => tools.GetDeletedRecords("both", "2025-03-01"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task Entity_count_is_limited(int count) =>
        await ToolTestHelpers.RejectsAsync(() => new ChangeTools(new FixtureQbClient()).GetChangesSince("2025-03-01", Enumerable.Repeat("Customer", count).ToArray()));

    [Fact]
    public void Timestamps_accept_dates_and_date_times()
    {
        Assert.Equal("2025-03-01", QueryFilters.Timestamp("2025-03-01"));
        Assert.Equal("2025-03-01T08:30:00", QueryFilters.Timestamp("2025-03-01T08:30"));
        Assert.Throws<ArgumentException>(() => QueryFilters.Timestamp("soon"));
        Assert.Throws<ArgumentException>(() => QueryFilters.Timestamp("T25:99"));
    }
}
