using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Core.Tests;

public class ReportTests
{
    [Fact]
    public void Find_ignores_case_and_punctuation()
    {
        Assert.Equal("ProfitAndLossStandard", ReportKind.Find("profit & loss standard")!.Value.Type);
        Assert.Equal("AgingReportQueryRq", ReportKind.Find("ar_aging_summary")!.Value.Kind.Request);
        Assert.Null(ReportKind.Find("nope"));
    }

    [Fact]
    public void Options_follow_schema_order()
    {
        var (kind, type) = ReportKind.Find("ProfitAndLossStandard")!.Value;
        var options = new ReportOptions { SummarizeColumnsBy = "Month", Basis = "cash", Classes = ["East"], From = "2025-01-01", To = "2025-03-31" };

        var names = options.ToElements(kind, type).Select(e => e.Name.LocalName);

        Assert.Equal(["GeneralSummaryReportType", "ReportPeriod", "ReportClassFilter", "ReportBasis", "SummarizeColumnsBy"], names);
    }

    [Fact]
    public void Aging_uses_as_of_not_basis()
    {
        var (kind, type) = ReportKind.Find("ARAgingSummary")!.Value;

        var names = new ReportOptions { DateMacro = "today", AgingAsOf = "ReportEndDate", Basis = "Cash" }
            .ToElements(kind, type).Select(e => e.Name.LocalName);

        Assert.Equal(["AgingReportType", "ReportDateMacro", "ReportAgingAsOf"], names);
    }

    [Fact]
    public void Bad_option_values_are_rejected()
    {
        var (kind, type) = ReportKind.Find("TrialBalance")!.Value;
        Assert.Throws<ArgumentException>(() => new ReportOptions { Basis = "Weird" }.ToElements(kind, type));
        Assert.Throws<ArgumentException>(() => new ReportOptions { DateMacro = "LastDecade" }.ToElements(kind, type));
    }

    [Fact]
    public async Task Runner_returns_a_table_with_flagged_rows()
    {
        var result = await new ReportRunner(new FixtureQbClient()).RunAsync("ARAgingSummary", new ReportOptions());

        Assert.Equal("A/R Aging Summary", (string?)result["title"]);
        Assert.Equal(3, result["columns"]!.AsArray().Count);
        var rows = result["rows"]!.AsArray();
        Assert.Equal(["data", "text", "total"], rows.Select(r => (string?)r!["kind"]));
        Assert.Equal(["Acme", "100.00", "100.00"], rows[0]!["cells"]!.AsArray().Select(c => (string?)c));
        Assert.Equal("Other customers", (string?)rows[1]!["cells"]![0]);
    }
}
