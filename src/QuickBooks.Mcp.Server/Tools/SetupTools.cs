using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Company preferences, chart of accounts and the other setup lists.</summary>
[McpServerToolType]
public sealed class SetupTools(IQbClient qb)
{
    private static readonly string[] SetupLists =
    [
        "Class", "Currency", "StandardTerms", "DateDrivenTerms", "PaymentMethod", "ShipMethod", "CustomerMsg",
        "SalesTaxCode", "SalesRep", "Template", "UnitOfMeasureSet", "Vehicle", "BillingRate", "PriceLevel",
        "WorkersCompCode", "CustomerType", "VendorType", "JobType",
    ];

    /// <summary>Returns the company preferences.</summary>
    [McpServerTool(Name = "get_preferences", ReadOnly = true)]
    [Description("Get the company preferences (accounting, sales tax, multi-currency, time tracking and more).")]
    public Task<string> GetPreferences(CancellationToken cancellationToken) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("PreferencesQueryRq", cancellationToken: cancellationToken)).Records));

    /// <summary>Searches the chart of accounts.</summary>
    [McpServerTool(Name = "search_accounts", ReadOnly = true)]
    [Description("Search the chart of accounts by name. Returns account type, number and balance.")]
    public Task<string> SearchAccounts(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Return only these fields, e.g. Name, AccountType, Balance.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, "Account", null,
            new QueryFilters { NameContains = nameContains, Active = active, Fields = fields },
            maxResults, cursor, cancellationToken);

    /// <summary>Searches one of the smaller setup lists.</summary>
    [McpServerTool(Name = "search_setup_lists", ReadOnly = true)]
    [Description("Search a setup list: Class, Currency, StandardTerms, DateDrivenTerms, PaymentMethod, ShipMethod, CustomerMsg, SalesTaxCode, SalesRep, Template, UnitOfMeasureSet, Vehicle, BillingRate, PriceLevel, WorkersCompCode, CustomerType, VendorType or JobType.")]
    public Task<string> SearchSetupLists(
        [Description("Which list to search.")] string list,
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Return only these fields.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, list, SetupLists,
            new QueryFilters { NameContains = nameContains, Active = active, Fields = fields },
            maxResults, cursor, cancellationToken);
}
