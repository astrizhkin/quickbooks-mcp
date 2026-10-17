using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Reports, returned as compact tables.</summary>
[McpServerToolType]
public sealed class ReportTools(IQbClient qb)
{
    /// <summary>Accounts receivable aging.</summary>
    [McpServerTool(Name = "get_aged_receivables", ReadOnly = true)]
    [Description("Get the accounts receivable aging (who owes money and how overdue it is), as a summary or detail table.")]
    public Task<string> GetAgedReceivables(
        [Description("true for invoice-level detail, false (default) for one row per customer.")] bool detail = false,
        [Description("Age as of this date (yyyy-MM-dd); default today.")] string? asOfDate = null,
        [Description("Only these customers (full names).")] string[]? customers = null,
        CancellationToken cancellationToken = default) =>
        Aging(detail ? "ARAgingDetail" : "ARAgingSummary", asOfDate, customers, cancellationToken);

    /// <summary>Accounts payable aging.</summary>
    [McpServerTool(Name = "get_aged_payables", ReadOnly = true)]
    [Description("Get the accounts payable aging (what the company owes vendors and how overdue it is), as a summary or detail table.")]
    public Task<string> GetAgedPayables(
        [Description("true for bill-level detail, false (default) for one row per vendor.")] bool detail = false,
        [Description("Age as of this date (yyyy-MM-dd); default today.")] string? asOfDate = null,
        [Description("Only these vendors (full names).")] string[]? vendors = null,
        CancellationToken cancellationToken = default) =>
        Aging(detail ? "APAgingDetail" : "APAgingSummary", asOfDate, vendors, cancellationToken);

    private Task<string> Aging(string type, string? asOfDate, string[]? entities, CancellationToken cancellationToken) =>
        Run(type, new ReportOptions
        {
            From = asOfDate,
            To = asOfDate,
            DateMacro = asOfDate is null ? "Today" : null,
            AgingAsOf = "ReportEndDate",
            Entities = entities,
        }, cancellationToken);

    private Task<string> Run(string type, ReportOptions options, CancellationToken cancellationToken) =>
        ToolErrors.RunAsync(async () =>
            (await new ReportRunner(qb).RunAsync(type, options, cancellationToken)).ToJsonString(new() { WriteIndented = true }));
}
