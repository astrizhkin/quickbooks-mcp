using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>The generic query tool.</summary>
[McpServerToolType]
public sealed class QueryTools(IQbClient qb, ServerConfig? config = null)
{
    /// <summary>Queries any supported entity.</summary>
    [McpServerTool(Name = "query_records", ReadOnly = true)]
    [Description("Search any QuickBooks entity (Customer, Invoice, Bill...) with filters; returns records and a next_cursor.")]
    public Task<string> QueryRecords(
        [Description("Entity name, e.g. Customer, Vendor, Employee, Account, Item, Class, Invoice, Bill, Check, Estimate, SalesOrder, PurchaseOrder, JournalEntry.")] string entity,
        [Description("ListID values (lists) or TxnID values (transactions).")] string[]? ids = null,
        [Description("Exact full names (lists only), like Smith:Kitchen Remodel.")] string[]? fullNames = null,
        [Description("Lists only: active (default), inactive or all.")] string? active = null,
        [Description("Lists only: name contains this text.")] string? nameContains = null,
        [Description("Lists only: name starts with this text.")] string? nameStartsWith = null,
        [Description("Last modified on or after this date (yyyy-MM-dd).")] string? modifiedFrom = null,
        [Description("Last modified on or before this date (yyyy-MM-dd).")] string? modifiedTo = null,
        [Description("Transactions only: transaction date on or after (yyyy-MM-dd).")] string? txnDateFrom = null,
        [Description("Transactions only: transaction date on or before (yyyy-MM-dd).")] string? txnDateTo = null,
        [Description("Transactions only: customer, vendor or employee full name.")] string? entityName = null,
        [Description("Transactions only: account full name.")] string? accountName = null,
        [Description("Transactions only: include line items.")] bool includeLineItems = false,
        [Description("Transactions only: include linked transactions.")] bool includeLinkedTxns = false,
        [Description("Return only these fields, e.g. Name, Balance. Saves tokens.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call; repeat the same filters.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var found = Entity.Find(entity)
                ?? throw new McpException($"Unknown entity '{entity}'. Use one of: {string.Join(", ", Entity.All.Select(e => e.Name))}.");
            if (found.Name.StartsWith("PayrollItem", StringComparison.Ordinal) && config?.AllowPayroll != true)
                throw new McpException("Payroll data is switched off. Start the server with --allow-payroll (QB_ALLOW_PAYROLL=true) to use it.");
            var filters = new QueryFilters
            {
                Ids = ids,
                FullNames = fullNames,
                Active = active,
                NameContains = nameContains,
                NameStartsWith = nameStartsWith,
                ModifiedFrom = modifiedFrom,
                ModifiedTo = modifiedTo,
                TxnDateFrom = txnDateFrom,
                TxnDateTo = txnDateTo,
                EntityName = entityName,
                AccountName = accountName,
                IncludeLineItems = includeLineItems,
                IncludeLinkedTxns = includeLinkedTxns,
                Fields = fields,
                IncludeCustomFields = includeCustomFields,
            };
            var result = await new QueryRunner(qb).RunAsync(found, filters, maxResults, cursor, cancellationToken);
            return result.ToJsonString(new() { WriteIndented = true });
        });
}
