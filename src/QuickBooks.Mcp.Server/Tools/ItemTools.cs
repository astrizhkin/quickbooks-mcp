using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Products, services and other items.</summary>
[McpServerToolType]
public sealed class ItemTools(IQbClient qb)
{
    private static readonly string[] Types =
    [
        "Service", "NonInventory", "Inventory", "InventoryAssembly", "OtherCharge", "Discount",
        "SalesTax", "SalesTaxGroup", "Group", "Subtotal", "Payment", "FixedAsset",
    ];

    /// <summary>Searches items, optionally of one type.</summary>
    [McpServerTool(Name = "search_items", ReadOnly = true)]
    [Description("Search items by name. Inventory items include quantities on hand and assemblies include how many can be built. Optionally limit to one item type.")]
    public Task<string> SearchItems(
        [Description("Limit to one type: Service, NonInventory, Inventory, InventoryAssembly, OtherCharge, Discount, SalesTax, SalesTaxGroup, Group, Subtotal, Payment or FixedAsset. Omit for all types.")] string? itemType = null,
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("Exact full names, like Parent:Child.")] string[]? fullNames = null,
        [Description("ListID values.")] string[]? ids = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Modified on or after this date (yyyy-MM-dd).")] string? modifiedFrom = null,
        [Description("Return only these fields, e.g. Name, QuantityOnHand.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default)
    {
        var entity = itemType is null ? "Item" : itemType.StartsWith("Item", StringComparison.OrdinalIgnoreCase) ? itemType : "Item" + itemType;
        string[] allowed = ["Item", .. Types.Select(t => "Item" + t)];
        return Search.RunAsync(qb, entity, allowed,
            new QueryFilters
            {
                NameContains = nameContains,
                FullNames = fullNames,
                Ids = ids,
                Active = active,
                ModifiedFrom = modifiedFrom,
                Fields = fields,
                IncludeCustomFields = includeCustomFields,
            },
            maxResults, cursor, cancellationToken);
    }
}
