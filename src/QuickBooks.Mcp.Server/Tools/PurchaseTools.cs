using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Purchase and banking transactions.</summary>
[McpServerToolType]
public sealed class PurchaseTools(IQbClient qb)
{
    private static readonly string[] Types =
    [
        "Bill", "BillPaymentCheck", "BillPaymentCreditCard", "PurchaseOrder", "ItemReceipt", "VendorCredit", "Check",
        "CreditCardCharge", "CreditCardCredit", "SalesTaxPaymentCheck", "Transfer", "JournalEntry",
    ];

    /// <summary>Searches purchase and banking transactions of one type.</summary>
    [McpServerTool(Name = "search_purchase_transactions", ReadOnly = true)]
    [Description("Search purchase and banking transactions of one type: Bill, BillPaymentCheck, BillPaymentCreditCard, PurchaseOrder, ItemReceipt, VendorCredit, Check, CreditCardCharge, CreditCardCredit, SalesTaxPaymentCheck, Transfer or JournalEntry.")]
    public Task<string> SearchPurchaseTransactions(
        [Description("Transaction type.")] string type,
        [Description("Vendor (or payee) full name on the transaction.")] string? vendor = null,
        [Description("Account full name on the transaction.")] string? account = null,
        [Description("Transaction date on or after (yyyy-MM-dd).")] string? dateFrom = null,
        [Description("Transaction date on or before (yyyy-MM-dd).")] string? dateTo = null,
        [Description("TxnID values.")] string[]? ids = null,
        [Description("Include line items.")] bool includeLineItems = false,
        [Description("Include linked transactions.")] bool includeLinkedTxns = false,
        [Description("Return only these fields, e.g. TxnDate, RefNumber, AmountDue.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, type, Types,
            new QueryFilters
            {
                EntityName = vendor,
                AccountName = account,
                TxnDateFrom = dateFrom,
                TxnDateTo = dateTo,
                Ids = ids,
                IncludeLineItems = includeLineItems,
                IncludeLinkedTxns = includeLinkedTxns,
                Fields = fields,
            },
            maxResults, cursor, cancellationToken);

    /// <summary>Searches every transaction type at once.</summary>
    [McpServerTool(Name = "search_transactions", ReadOnly = true)]
    [Description("Search transactions of all types at once by date, name or account. Returns summaries; use the typed search tools for full detail.")]
    public Task<string> SearchTransactions(
        [Description("Customer, vendor or employee full name.")] string? name = null,
        [Description("Account full name.")] string? account = null,
        [Description("Transaction date on or after (yyyy-MM-dd).")] string? dateFrom = null,
        [Description("Transaction date on or before (yyyy-MM-dd).")] string? dateTo = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, "Transaction", ["Transaction"],
            new QueryFilters { EntityName = name, AccountName = account, TxnDateFrom = dateFrom, TxnDateTo = dateTo },
            maxResults, cursor, cancellationToken);
}
