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

public class SummaryDetailReportToolsTests
{
    [Theory]
    [InlineData("pnl", "GeneralSummaryReportQueryRq", "ProfitAndLossStandard")]
    [InlineData("bs", "GeneralSummaryReportQueryRq", "BalanceSheetStandard")]
    [InlineData("tb", "GeneralSummaryReportQueryRq", "TrialBalance")]
    [InlineData("gl", "GeneralDetailReportQueryRq", "GeneralLedger")]
    [InlineData("other", "GeneralSummaryReportQueryRq", "SalesByCustomerSummary")]
    public async Task Each_tool_requests_its_report(string tool, string request, string type)
    {
        var qb = new FixtureQbClient();
        var tools = new ReportTools(qb);

        var json = await (tool switch
        {
            "pnl" => tools.GetProfitAndLoss("ThisMonth"),
            "bs" => tools.GetBalanceSheet("ThisMonthToDate"),
            "tb" => tools.GetTrialBalance("ThisYearToDate"),
            "gl" => tools.GetGeneralLedger("LastMonth", accounts: ["Checking"]),
            _ => tools.GetReport("SalesByCustomerSummary", "ThisYear", summarizeColumnsBy: "Month"),
        });

        Assert.Equal(request, qb.SentRequest());
        Assert.Contains(type, qb.Requests[0].ToString());
        Assert.Contains("Acme", json);
    }

    [Fact]
    public async Task Unknown_report_lists_valid_names()
    {
        var ex = await ToolTestHelpers.RejectsAsync(() => new ReportTools(new FixtureQbClient()).GetReport("Nope"));
        Assert.Contains("TrialBalance", ex.Message);
    }

    [Fact]
    public async Task Layout_selects_the_variant()
    {
        var qb = new FixtureQbClient();

        await new ReportTools(qb).GetProfitAndLoss("ThisYear", layout: "ByClass");

        Assert.Contains("ProfitAndLossByClass", qb.Requests[0].ToString());
    }
}

public class CustomReportToolsTests
{
    [Fact]
    public async Task Custom_options_reach_the_request()
    {
        var qb = new FixtureQbClient();

        await new ReportTools(qb).GetReport("CustomSummary", "ThisYear", summarizeRowsBy: "Account", includeColumns: ["Amount"]);

        Assert.Contains("SummarizeRowsBy", qb.Requests[0].ToString());
        Assert.Contains("IncludeColumn", qb.Requests[0].ToString());
    }
}

public class ReportFormatTests
{
    [Fact]
    public async Task Markdown_format_returns_a_table() =>
        Assert.Contains("| Customer |", await new ReportTools(new FixtureQbClient()).GetAgedReceivables(format: "markdown"));

    [Fact]
    public async Task Unknown_format_is_a_tool_error() =>
        await ToolTestHelpers.RejectsAsync(() => new ReportTools(new FixtureQbClient()).GetAgedReceivables(format: "xml"));
}
