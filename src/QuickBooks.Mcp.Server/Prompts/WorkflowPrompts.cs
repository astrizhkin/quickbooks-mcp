using System.ComponentModel;
using ModelContextProtocol.Server;

namespace QuickBooks.Mcp.Server.Prompts;

/// <summary>Ready-made starting points for common bookkeeping workflows.</summary>
[McpServerPromptType]
public sealed class WorkflowPrompts
{
    /// <summary>Month-end review.</summary>
    [McpServerPrompt(Name = "month_end_review")]
    [Description("Review a month's books for problems before closing.")]
    public static string MonthEndReview([Description("Month to review, like 2025-03.")] string month) => $"""
        Review QuickBooks for {month} before closing the month. Read only; do not change anything.
        1. Search journal entries and checks dated in {month}; list unusual or round-number ones.
        2. Search invoices and bills for {month}; flag any dated outside the month or with missing references.
        3. Check the chart of accounts for accounts with unexpected balances (negative assets, large Uncategorized or Ask My Accountant).
        4. Summarize findings as a short checklist with the transaction numbers to look at.
        """;

    /// <summary>Accounts receivable follow-up.</summary>
    [McpServerPrompt(Name = "ar_collections_follow_up")]
    [Description("Draft a collections plan for overdue customer invoices.")]
    public static string ArCollectionsFollowUp([Description("Only invoices overdue by at least this many days.")] int minDaysOverdue = 30) => $"""
        Find customers with invoices overdue by {minDaysOverdue} days or more.
        Search open invoices (use linked transactions to see partial payments), group them by customer, and rank by amount and age.
        For the top customers, draft a short, polite follow-up message that names the invoice numbers and amounts. Do not send anything.
        """;

    /// <summary>Duplicate vendor detection.</summary>
    [McpServerPrompt(Name = "find_duplicate_vendors")]
    [Description("Find vendors that look like duplicates of each other.")]
    public static string FindDuplicateVendors() => """
        List all vendors, including inactive ones. Find groups that are probably the same vendor: names differing only in punctuation, case, suffixes such as Inc or LLC, or small typos, or sharing a phone number or address.
        For each group show the names, balances and the most recent transaction date, and suggest which one to keep. Do not change anything.
        """;
}
