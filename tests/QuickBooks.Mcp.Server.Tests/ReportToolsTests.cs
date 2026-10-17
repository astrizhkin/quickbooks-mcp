using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class ReportToolsTests
{
    [Theory]
    [InlineData(false, "ARAgingSummary")]
    [InlineData(true, "ARAgingDetail")]
    public async Task Aged_receivables_pick_summary_or_detail(bool detail, string type)
    {
        var qb = new FixtureQbClient();

        var json = await new ReportTools(qb).GetAgedReceivables(detail, asOfDate: "2025-01-31");

        Assert.Equal("AgingReportQueryRq", qb.SentRequest());
        Assert.Contains(type, qb.Requests[0].ToString());
        Assert.Contains("Acme", json);
    }

    [Fact]
    public async Task Aged_payables_use_the_ap_report()
    {
        var qb = new FixtureQbClient();

        await new ReportTools(qb).GetAgedPayables();

        Assert.Contains("APAgingSummary", qb.Requests[0].ToString());
    }

    [Fact]
    public async Task Bad_date_is_a_tool_error() =>
        await ToolTestHelpers.RejectsAsync(() => new ReportTools(new FixtureQbClient()).GetAgedReceivables(asOfDate: "soon"));
}
