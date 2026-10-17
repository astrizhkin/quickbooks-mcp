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
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Aging(detail ? "ARAgingDetail" : "ARAgingSummary", asOfDate, customers, format, cancellationToken);

    /// <summary>Accounts payable aging.</summary>
    [McpServerTool(Name = "get_aged_payables", ReadOnly = true)]
    [Description("Get the accounts payable aging (what the company owes vendors and how overdue it is), as a summary or detail table.")]
    public Task<string> GetAgedPayables(
        [Description("true for bill-level detail, false (default) for one row per vendor.")] bool detail = false,
        [Description("Age as of this date (yyyy-MM-dd); default today.")] string? asOfDate = null,
        [Description("Only these vendors (full names).")] string[]? vendors = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Aging(detail ? "APAgingDetail" : "APAgingSummary", asOfDate, vendors, format, cancellationToken);

    /// <summary>Profit and loss.</summary>
    [McpServerTool(Name = "get_profit_and_loss", ReadOnly = true)]
    [Description("Get the profit and loss (income statement) for a period, optionally by month, class or job.")]
    public Task<string> GetProfitAndLoss(
        [Description("Period macro like ThisMonth, LastMonth, ThisQuarterToDate, ThisYearToDate, LastYear. Or use from/to.")] string? dateMacro = null,
        [Description("Period start (yyyy-MM-dd).")] string? from = null,
        [Description("Period end (yyyy-MM-dd).")] string? to = null,
        [Description("Accrual or Cash; default is the company setting.")] string? basis = null,
        [Description("Standard (default), ByClass, ByJob, PrevYearComp or YTDComp.")] string layout = "Standard",
        [Description("Column grouping such as Month, Quarter, Year or TotalOnly.")] string? summarizeColumnsBy = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Run("ProfitAndLoss" + layout, new ReportOptions
        {
            DateMacro = dateMacro,
            From = from,
            To = to,
            Basis = basis,
            SummarizeColumnsBy = summarizeColumnsBy,
        }, format, cancellationToken);

    /// <summary>Balance sheet.</summary>
    [McpServerTool(Name = "get_balance_sheet", ReadOnly = true)]
    [Description("Get the balance sheet as of the end of a period.")]
    public Task<string> GetBalanceSheet(
        [Description("Period macro like ThisMonthToDate or LastYear. Or use to.")] string? dateMacro = null,
        [Description("Report as of this date (yyyy-MM-dd).")] string? to = null,
        [Description("Accrual or Cash.")] string? basis = null,
        [Description("Standard (default), Summary, PrevYearComp or ByClass.")] string layout = "Standard",
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Run("BalanceSheet" + layout, new ReportOptions { DateMacro = dateMacro, To = to, Basis = basis }, format, cancellationToken);

    /// <summary>Trial balance.</summary>
    [McpServerTool(Name = "get_trial_balance", ReadOnly = true)]
    [Description("Get the trial balance for a period.")]
    public Task<string> GetTrialBalance(
        [Description("Period macro like ThisYearToDate. Or use from/to.")] string? dateMacro = null,
        [Description("Period start (yyyy-MM-dd).")] string? from = null,
        [Description("Period end (yyyy-MM-dd).")] string? to = null,
        [Description("Accrual or Cash.")] string? basis = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Run("TrialBalance", new ReportOptions { DateMacro = dateMacro, From = from, To = to, Basis = basis }, format, cancellationToken);

    /// <summary>General ledger.</summary>
    [McpServerTool(Name = "get_general_ledger", ReadOnly = true)]
    [Description("Get the general ledger (every posting by account) for a period. Can be large; filter by account.")]
    public Task<string> GetGeneralLedger(
        [Description("Period macro like LastMonth. Or use from/to.")] string? dateMacro = null,
        [Description("Period start (yyyy-MM-dd).")] string? from = null,
        [Description("Period end (yyyy-MM-dd).")] string? to = null,
        [Description("Only these accounts (full names).")] string[]? accounts = null,
        [Description("Accrual or Cash.")] string? basis = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Run("GeneralLedger", new ReportOptions { DateMacro = dateMacro, From = from, To = to, Accounts = accounts, Basis = basis }, format, cancellationToken);

    /// <summary>Any other summary or detail report.</summary>
    [McpServerTool(Name = "get_report", ReadOnly = true)]
    [Description("Get any other QuickBooks report by type, e.g. SalesByCustomerSummary, CustomerBalanceSummary, VendorBalanceSummary, ExpenseByVendorSummary, SalesByItemDetail, OpenInvoices, TxnListByDate, JobProfitabilitySummary, TimeByName, ProfitAndLossBudgetVsActual.")]
    public Task<string> GetReport(
        [Description("Report type name; an unknown name returns the full list.")] string reportType,
        [Description("Period macro like ThisMonth. Or use from/to.")] string? dateMacro = null,
        [Description("Period start (yyyy-MM-dd).")] string? from = null,
        [Description("Period end (yyyy-MM-dd).")] string? to = null,
        [Description("Accrual or Cash.")] string? basis = null,
        [Description("Column grouping such as Month, Customer, Class or TotalOnly.")] string? summarizeColumnsBy = null,
        [Description("Only these accounts (full names).")] string[]? accounts = null,
        [Description("Only these customers, vendors or employees (full names).")] string[]? names = null,
        [Description("Only these items (full names).")] string[]? items = null,
        [Description("Only these classes (full names).")] string[]? classes = null,
        [Description("Posting, NonPosting or Either.")] string? postingStatus = null,
        [Description("Custom reports: row grouping such as Account, Customer, Item or Month.")] string? summarizeRowsBy = null,
        [Description("Budget reports (required for them): fiscal year, e.g. 2025.")] int? fiscalYear = null,
        [Description("Budget reports: Accounts, AccountsAndClasses or AccountsAndCustomers.")] string? budgetCriterion = null,
        [Description("Detail and custom reports: extra columns such as Amount, Date, Memo, Name, Num, Balance.")] string[]? includeColumns = null,
        [Description("json (default) or markdown table.")] string format = "json",
        CancellationToken cancellationToken = default) =>
        Run(reportType, new ReportOptions
        {
            DateMacro = dateMacro,
            From = from,
            To = to,
            Basis = basis,
            SummarizeColumnsBy = summarizeColumnsBy,
            Accounts = accounts,
            Entities = names,
            Items = items,
            Classes = classes,
            PostingStatus = postingStatus,
            SummarizeRowsBy = summarizeRowsBy,
            IncludeColumns = includeColumns,
            FiscalYear = fiscalYear,
            BudgetCriterion = budgetCriterion,
        }, format, cancellationToken);

    private Task<string> Aging(string type, string? asOfDate, string[]? entities, string format, CancellationToken cancellationToken) =>
        Run(type, new ReportOptions
        {
            From = asOfDate,
            To = asOfDate,
            DateMacro = asOfDate is null ? "Today" : null,
            AgingAsOf = "ReportEndDate",
            Entities = entities,
        }, format, cancellationToken);

    private Task<string> Run(string type, ReportOptions options, string format, CancellationToken cancellationToken) =>
        ToolErrors.RunAsync(async () =>
        {
            var report = await new ReportRunner(qb).RunAsync(type, options, cancellationToken);
            return format.ToLowerInvariant() switch
            {
                "json" => report.ToJsonString(new() { WriteIndented = true }),
                "markdown" => ReportMarkdown.Render(report),
                _ => throw new ArgumentException($"format must be json or markdown, not '{format}'."),
            };
        });
}
