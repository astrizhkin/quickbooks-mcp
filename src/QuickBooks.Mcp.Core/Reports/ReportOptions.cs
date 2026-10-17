using System.Globalization;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.Reports;

/// <summary>Options shared by all report requests; each is added only when set.</summary>
public sealed class ReportOptions
{
    /// <summary>Report period macro, e.g. ThisMonth, LastQuarterToDate, ThisYearToDate.</summary>
    public string? DateMacro { get; init; }

    /// <summary>Period start (yyyy-MM-dd); used with <see cref="To"/> instead of a macro.</summary>
    public string? From { get; init; }

    /// <summary>Period end (yyyy-MM-dd).</summary>
    public string? To { get; init; }

    /// <summary>Accrual or Cash.</summary>
    public string? Basis { get; init; }

    /// <summary>Column grouping, e.g. Month, Quarter, Year, Customer, Class, TotalOnly.</summary>
    public string? SummarizeColumnsBy { get; init; }

    /// <summary>Row grouping for custom reports, e.g. Account, Customer, Item, Month.</summary>
    public string? SummarizeRowsBy { get; init; }

    /// <summary>Extra columns for detail and custom reports, e.g. Amount, Date, Memo, Name, Num, Balance.</summary>
    public IReadOnlyList<string>? IncludeColumns { get; init; }

    /// <summary>Budget reports (required for them): the fiscal year, e.g. 2025.</summary>
    public int? FiscalYear { get; init; }

    /// <summary>Budget reports: Accounts, AccountsAndClasses or AccountsAndCustomers.</summary>
    public string? BudgetCriterion { get; init; }

    /// <summary>Aging reports: ReportEndDate (default) or Today.</summary>
    public string? AgingAsOf { get; init; }

    /// <summary>Full names to filter by account.</summary>
    public IReadOnlyList<string>? Accounts { get; init; }

    /// <summary>Full names to filter by customer, vendor or employee.</summary>
    public IReadOnlyList<string>? Entities { get; init; }

    /// <summary>Full names to filter by item.</summary>
    public IReadOnlyList<string>? Items { get; init; }

    /// <summary>Full names to filter by class.</summary>
    public IReadOnlyList<string>? Classes { get; init; }

    /// <summary>Posting status: Posting, NonPosting or Either.</summary>
    public string? PostingStatus { get; init; }

    private static readonly string[] Macros =
    [
        "All", "Today", "ThisWeek", "ThisWeekToDate", "ThisMonth", "ThisMonthToDate", "ThisQuarter", "ThisQuarterToDate",
        "ThisYear", "ThisYearToDate", "Yesterday", "LastWeek", "LastWeekToDate", "LastMonth", "LastMonthToDate",
        "LastQuarter", "LastQuarterToDate", "LastYear", "LastYearToDate", "NextWeek", "NextFourWeeks", "NextMonth",
        "NextQuarter", "NextYear",
    ];

    /// <summary>
    /// Builds the request children in the order the qbXML schema requires. Each report family accepts
    /// a different subset, and QuickBooks rejects a request with an element it does not know, so an
    /// option the family does not support is refused with a clear message.
    /// </summary>
    public List<XElement> ToElements(ReportKind kind, string type)
    {
        var request = kind.Request;
        var (account, posting, columnsBy, rowsBy, includeColumns, basis, agingAsOf) = request switch
        {
            "AgingReportQueryRq" => (true, true, false, false, true, false, true),
            "GeneralSummaryReportQueryRq" => (true, true, true, false, false, true, false),
            "GeneralDetailReportQueryRq" => (true, true, false, true, true, true, false),
            "JobReportQueryRq" => (true, true, true, false, false, false, false),
            "TimeReportQueryRq" => (false, false, true, false, true, false, false),
            "BudgetSummaryReportQueryRq" => (false, false, true, true, false, false, false),
            "PayrollSummaryReportQueryRq" => (true, true, true, false, false, false, false),
            "PayrollDetailReportQueryRq" => (true, true, false, true, true, false, false),
            "CustomSummaryReportQueryRq" => (true, true, true, true, false, true, false),
            _ => (true, true, false, true, true, true, false), // CustomDetailReportQueryRq
        };

        Refuse(Accounts, account, "accounts", type);
        Refuse(PostingStatus, posting, "postingStatus", type);
        Refuse(SummarizeColumnsBy, columnsBy, "summarizeColumnsBy", type);
        Refuse(SummarizeRowsBy, rowsBy, "summarizeRowsBy", type);
        Refuse(IncludeColumns, includeColumns, "includeColumns", type);
        Refuse(Basis, basis, "basis", type);
        Refuse(AgingAsOf, agingAsOf, "agingAsOf", type);
        Refuse(Entities, request != "BudgetSummaryReportQueryRq", "names", type);
        Refuse(Items, request != "BudgetSummaryReportQueryRq", "items", type);

        var budget = request == "BudgetSummaryReportQueryRq";
        Refuse(FiscalYear, budget, "fiscalYear", type);
        Refuse(BudgetCriterion, budget, "budgetCriterion", type);
        if (budget && FiscalYear is null) throw new ArgumentException($"{type} needs fiscalYear, e.g. 2025.");

        // QuickBooks answers these with a bare "missing element" or "cannot use element" status, so say what is needed.
        if (type == "PhysicalInventoryWorksheet" && (DateMacro is not null || From is not null || To is not null))
            throw new ArgumentException($"{type} does not take a period; leave out dateMacro, from and to.");
        if (type == "MissingChecks" && Accounts is not { Count: > 0 })
            throw new ArgumentException($"{type} needs accounts (the bank account to check).");
        if (type is "JobEstimatesVsActualsDetail" or "JobProfitabilityDetail" && Entities is not { Count: > 0 })
            throw new ArgumentException($"{type} needs names (a customer or job).");

        if (request == "CustomSummaryReportQueryRq" && (SummarizeColumnsBy is null || SummarizeRowsBy is null))
            throw new ArgumentException($"{type} needs summarizeColumnsBy and summarizeRowsBy.");
        if (request == "CustomDetailReportQueryRq" && (SummarizeRowsBy is null || IncludeColumns is not { Count: > 0 }))
            throw new ArgumentException($"{type} needs summarizeRowsBy and at least one includeColumns value.");

        var e = new List<XElement> { new(kind.TypeElement, type) };
        if (budget)
        {
            e.Add(new XElement("FiscalYear", FiscalYear));
            if (BudgetCriterion is not null) e.Add(new XElement("BudgetCriterion", Pick(BudgetCriterion, ["Accounts", "AccountsAndClasses", "AccountsAndCustomers"], nameof(BudgetCriterion))));
        }

        if (DateMacro is not null) e.Add(new XElement("ReportDateMacro", Pick(DateMacro, Macros, nameof(DateMacro))));
        else if (From is not null || To is not null)
            e.Add(new XElement("ReportPeriod",
                From is null ? null : new XElement("FromReportDate", Date(From)),
                To is null ? null : new XElement("ToReportDate", Date(To))));

        AddFilter(e, "ReportAccountFilter", Accounts);
        AddFilter(e, "ReportEntityFilter", Entities);
        AddFilter(e, "ReportItemFilter", Items);
        AddFilter(e, "ReportClassFilter", Classes);
        if (PostingStatus is not null) e.Add(new XElement("ReportPostingStatusFilter", Pick(PostingStatus, ["Posting", "NonPosting", "Either"], nameof(PostingStatus))));

        if (SummarizeColumnsBy is not null) e.Add(new XElement(budget ? "SummarizeBudgetColumnsBy" : "SummarizeColumnsBy", SummarizeColumnsBy));
        if (SummarizeRowsBy is not null) e.Add(new XElement(budget ? "SummarizeBudgetRowsBy" : "SummarizeRowsBy", SummarizeRowsBy));
        e.AddRange((IncludeColumns ?? []).Select(c => new XElement("IncludeColumn", c)));
        if (AgingAsOf is not null) e.Add(new XElement("ReportAgingAsOf", Pick(AgingAsOf, ["ReportEndDate", "Today"], nameof(AgingAsOf))));
        if (Basis is not null) e.Add(new XElement("ReportBasis", Pick(Basis, ["Accrual", "Cash"], nameof(Basis))));

        return e;
    }

    private static void Refuse(object? value, bool supported, string option, string type)
    {
        if (value is not null && !supported && !(value is IReadOnlyCollection<string> { Count: 0 }))
            throw new ArgumentException($"{type} does not support {option}.");
    }

    private static void AddFilter(List<XElement> into, string name, IReadOnlyList<string>? fullNames)
    {
        if (fullNames is not { Count: > 0 }) return;
        into.Add(new XElement(name, fullNames.Select(n => new XElement("FullName", n))));
    }

    private static string Pick(string value, string[] allowed, string name) =>
        allowed.FirstOrDefault(a => a.Equals(value, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"{name} must be one of {string.Join(", ", allowed)}, not '{value}'.");

    private static string Date(string text) =>
        DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var d)
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : throw new ArgumentException($"'{text}' is not a date; use yyyy-MM-dd.");
}
