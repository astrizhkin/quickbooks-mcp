using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Payroll items and payroll reports. Registered only when payroll is switched on (QB_ALLOW_PAYROLL).</summary>
[McpServerToolType]
public sealed class PayrollTools(IQbClient qb)
{
    private static readonly string[] Items = ["PayrollItemWage", "PayrollItemNonWage"];

    /// <summary>Searches payroll items.</summary>
    [McpServerTool(Name = "search_payroll_items", ReadOnly = true)]
    [Description("Search payroll items: wage items (salary, hourly, overtime) or non-wage items (taxes, deductions, company contributions).")]
    public Task<string> SearchPayrollItems(
        [Description("PayrollItemWage (default) or PayrollItemNonWage.")] string type = "PayrollItemWage",
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, type, Items, new QueryFilters { NameContains = nameContains, Active = active }, maxResults, cursor, cancellationToken);

    /// <summary>Runs a payroll report.</summary>
    [McpServerTool(Name = "get_payroll_report", ReadOnly = true)]
    [Description("Get a payroll report: PayrollSummary, EmployeeEarningsSummary, PayrollLiabilityBalances, PayrollItemDetail, PayrollTransactionDetail, PayrollTransactionsByPayee, PayrollReviewDetail or EmployeeStateTaxesDetail.")]
    public Task<string> GetPayrollReport(
        [Description("Report type name.")] string reportType,
        [Description("Period macro like LastQuarter or ThisYearToDate. Or use from/to.")] string? dateMacro = null,
        [Description("Period start (yyyy-MM-dd).")] string? from = null,
        [Description("Period end (yyyy-MM-dd).")] string? to = null,
        [Description("Only these employees (full names).")] string[]? employees = null,
        [Description("Summary reports: column grouping such as Month, Employee or TotalOnly.")] string? summarizeColumnsBy = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var report = await new ReportRunner(qb, ReportKind.Payroll).RunAsync(reportType, new ReportOptions
            {
                DateMacro = dateMacro,
                From = from,
                To = to,
                Entities = employees,
                SummarizeColumnsBy = summarizeColumnsBy,
            }, cancellationToken);
            return format.ToLowerInvariant() switch
            {
                "json" => report.ToJsonString(new() { WriteIndented = true }),
                "markdown" => ReportMarkdown.Render(report),
                _ => throw new ArgumentException($"format must be json or markdown, not '{format}'."),
            };
        });
}
