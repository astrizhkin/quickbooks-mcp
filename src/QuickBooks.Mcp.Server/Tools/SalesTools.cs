using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Sales transactions: invoices, receipts, credit memos, estimates, orders and payments.</summary>
[McpServerToolType]
public sealed class SalesTools(IQbClient qb)
{
    private static readonly string[] Types =
        ["Invoice", "SalesReceipt", "CreditMemo", "Estimate", "SalesOrder", "ReceivePayment", "ARRefundCreditCard", "Charge", "Deposit"];

    /// <summary>Searches sales transactions of one type.</summary>
    [McpServerTool(Name = "search_sales_transactions", ReadOnly = true)]
    [Description("Search sales transactions of one type: Invoice, SalesReceipt, CreditMemo, Estimate, SalesOrder, ReceivePayment, ARRefundCreditCard, Charge or Deposit.")]
    public Task<string> SearchSalesTransactions(
        [Description("Transaction type.")] string type,
        [Description("Customer full name on the transaction.")] string? customer = null,
        [Description("Transaction date on or after (yyyy-MM-dd).")] string? dateFrom = null,
        [Description("Transaction date on or before (yyyy-MM-dd).")] string? dateTo = null,
        [Description("TxnID values.")] string[]? ids = null,
        [Description("Include line items.")] bool includeLineItems = false,
        [Description("Include linked transactions, e.g. payments applied to an invoice.")] bool includeLinkedTxns = false,
        [Description("Return only these fields, e.g. TxnDate, RefNumber, BalanceRemaining.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, type, Types,
            new QueryFilters
            {
                EntityName = customer,
                TxnDateFrom = dateFrom,
                TxnDateTo = dateTo,
                Ids = ids,
                IncludeLineItems = includeLineItems,
                IncludeLinkedTxns = includeLinkedTxns,
                Fields = fields,
                IncludeCustomFields = includeCustomFields,
            },
            maxResults, cursor, cancellationToken);
}
