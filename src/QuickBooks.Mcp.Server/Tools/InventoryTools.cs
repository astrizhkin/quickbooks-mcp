using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Inventory sites, stock by site, buildable assemblies and inventory transactions.</summary>
[McpServerToolType]
public sealed class InventoryTools(IQbClient qb)
{
    private static readonly string[] Transactions = ["InventoryAdjustment", "TransferInventory", "BuildAssembly"];

    private static readonly string[] ItemTypes =
        ["AllExceptFixedAsset", "Assembly", "Discount", "FixedAsset", "Inventory", "InventoryAndAssembly", "NonInventory", "OtherCharge", "Payment", "Sales", "SalesTax", "Service"];

    /// <summary>Searches inventory sites.</summary>
    [McpServerTool(Name = "search_inventory_sites", ReadOnly = true)]
    [Description("Search inventory sites (warehouses and locations) by name. Needs QuickBooks Enterprise with advanced inventory.")]
    public Task<string> SearchInventorySites(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, "InventorySite", null, new QueryFilters { NameContains = nameContains, Active = active }, maxResults, cursor, cancellationToken);

    /// <summary>Searches inventory adjustments, transfers and assembly builds.</summary>
    [McpServerTool(Name = "search_inventory_transactions", ReadOnly = true)]
    [Description("Search inventory transactions of one type: InventoryAdjustment, TransferInventory or BuildAssembly.")]
    public Task<string> SearchInventoryTransactions(
        [Description("InventoryAdjustment, TransferInventory or BuildAssembly.")] string type,
        [Description("Transaction date on or after (yyyy-MM-dd).")] string? dateFrom = null,
        [Description("Transaction date on or before (yyyy-MM-dd).")] string? dateTo = null,
        [Description("TxnID values.")] string[]? ids = null,
        [Description("Include line items.")] bool includeLineItems = false,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, type, Transactions,
            new QueryFilters { TxnDateFrom = dateFrom, TxnDateTo = dateTo, Ids = ids, IncludeLineItems = includeLineItems && type != "BuildAssembly" },
            maxResults, cursor, cancellationToken);

    /// <summary>Stock by item and site.</summary>
    [McpServerTool(Name = "get_item_sites", ReadOnly = true)]
    [Description("Get quantities on hand, on order and on sales orders per item and inventory site. Filter by item type, or by one item and/or one site.")]
    public Task<string> GetItemSites(
        [Description("Limit to a kind of item: Inventory, Assembly, InventoryAndAssembly, NonInventory, Service, ...; not with item or site.")] string? itemType = null,
        [Description("Full name of one item.")] string? item = null,
        [Description("Full name of one inventory site.")] string? site = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Maximum rows (default 100).")] int maxResults = 100,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            if (itemType is not null && (item is not null || site is not null))
                throw new ArgumentException("Use itemType or item/site, not both.");

            var elements = new List<XElement>();
            if (itemType is not null)
                elements.Add(new XElement("ItemTypeFilter", ItemTypes.FirstOrDefault(t => t.Equals(itemType, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"itemType must be one of {string.Join(", ", ItemTypes)}.")));
            else if (item is not null || site is not null)
                elements.Add(new XElement("ItemSiteFilter",
                    item is null ? null : new XElement("ItemFilter", new XElement("FullName", item)),
                    site is null ? null : new XElement("SiteFilter", new XElement("FullName", site))));

            elements.Add(new XElement("MaxReturned", Math.Clamp(maxResults, 1, QueryRunner.MaxPageSize)));
            elements.Add(new XElement("ActiveStatus", (active ?? "active").ToLowerInvariant() switch
            {
                "active" => "ActiveOnly",
                "inactive" => "InactiveOnly",
                "all" => "All",
                var other => throw new ArgumentException($"active must be active, inactive or all, not '{other}'."),
            }));
            return QbXmlJson.ToJson((await qb.SendAsync("ItemSitesQueryRq", elements, cancellationToken)).Records);
        });

    /// <summary>How many of an assembly can be built.</summary>
    [McpServerTool(Name = "get_assemblies_can_build", ReadOnly = true)]
    [Description("Get how many units of an inventory assembly can be built from the components on hand.")]
    public Task<string> GetAssembliesCanBuild(
        [Description("Full name of the inventory assembly item.")] string assembly,
        [Description("As of this date (yyyy-MM-dd); default today.")] string? asOfDate = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var date = asOfDate is null
                ? DateOnly.FromDateTime(DateTime.Today)
                : DateOnly.TryParse(asOfDate, CultureInfo.InvariantCulture, out var d) ? d : throw new ArgumentException($"'{asOfDate}' is not a date; use yyyy-MM-dd.");
            List<XElement> elements =
            [
                new("ItemInventoryAssemblyRef", new XElement("FullName", assembly)),
                new("TxnDate", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ];
            return QbXmlJson.ToJson((await qb.SendAsync("ItemAssembliesCanBuildQueryRq", elements, cancellationToken)).Records);
        });
}
