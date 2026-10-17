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

        Assert.Equal(["GeneralSummaryReportType", "ReportPeriod", "ReportClassFilter", "SummarizeColumnsBy", "ReportBasis"], names);
    }

    [Fact]
    public void Aging_uses_as_of_and_refuses_basis()
    {
        var (kind, type) = ReportKind.Find("ARAgingSummary")!.Value;

        var names = new ReportOptions { DateMacro = "today", AgingAsOf = "ReportEndDate" }
            .ToElements(kind, type).Select(e => e.Name.LocalName);

        Assert.Equal(["AgingReportType", "ReportDateMacro", "ReportAgingAsOf"], names);
    }

    [Fact]
    public void Options_a_report_family_lacks_are_refused()
    {
        var (aging, agingType) = ReportKind.Find("ARAgingSummary")!.Value;
        var (detail, detailType) = ReportKind.Find("GeneralLedger")!.Value;

        Assert.Throws<ArgumentException>(() => new ReportOptions { Basis = "Cash" }.ToElements(aging, agingType));
        Assert.Throws<ArgumentException>(() => new ReportOptions { SummarizeColumnsBy = "Month" }.ToElements(detail, detailType));
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

public class ReportDocsTests
{
    [Fact]
    public void Every_report_type_is_documented()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "docs", "REPORTS.md"))) dir = Path.GetDirectoryName(dir)!;
        var docs = File.ReadAllText(Path.Combine(dir, "docs", "REPORTS.md"));

        var missing = ReportKind.All.Concat(ReportKind.Payroll).SelectMany(k => k.Types).Where(t => !docs.Contains(t, StringComparison.Ordinal));

        Assert.Empty(missing);
    }

    [Fact]
    public void Custom_options_are_sent()
    {
        var (kind, type) = ReportKind.Find("CustomTxnDetail")!.Value;

        var names = new ReportOptions { SummarizeRowsBy = "Account", IncludeColumns = ["Amount"] }
            .ToElements(kind, type).Select(e => e.Name.LocalName);

        Assert.Equal(["CustomDetailReportType", "SummarizeRowsBy", "IncludeColumn"], names);
    }
}

public class ReportMarkdownTests
{
    [Fact]
    public async Task Markdown_has_header_aligned_amounts_and_bold_totals()
    {
        var table = await new ReportRunner(new FixtureQbClient()).RunAsync("ARAgingSummary", new ReportOptions());

        var md = ReportMarkdown.Render(table);

        Assert.Contains("# A/R Aging Summary", md);
        Assert.Contains("| Customer | Current | TOTAL |", md);
        Assert.Contains("| --- | ---: | ---: |", md);
        Assert.Contains("| Acme | 100.00 | 100.00 |", md);
        Assert.Contains("| **TOTAL** | **100.00** | **100.00** |", md);
        Assert.Contains("| *Other customers* |", md);
    }

    [Fact]
    public async Task Columns_carry_a_data_type()
    {
        var table = await new ReportRunner(new FixtureQbClient()).RunAsync("ARAgingSummary", new ReportOptions());

        Assert.Equal(["text", "amount", "amount"], table["columns"]!.AsArray().Select(c => (string?)c!["dataType"]));
    }
}

public class BudgetReportTests
{
    [Fact]
    public void Budget_reports_need_a_fiscal_year()
    {
        var (kind, type) = ReportKind.Find("ProfitAndLossBudgetVsActual")!.Value;

        Assert.Throws<ArgumentException>(() => new ReportOptions { DateMacro = "ThisYear" }.ToElements(kind, type));
        Assert.Equal(["BudgetSummaryReportType", "FiscalYear"],
            new ReportOptions { FiscalYear = 2025 }.ToElements(kind, type).Select(e => e.Name.LocalName));
    }
}
