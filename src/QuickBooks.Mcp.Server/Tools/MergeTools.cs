using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Merging list items: a write that cannot be undone and removes a record.</summary>
[McpServerToolType]
public sealed class MergeTools(IQbClient qb, WriteExecutor executor)
{
    private static readonly string[] Types =
    [
        "Account", "Class", "Customer", "ItemDiscount", "ItemGroup", "ItemInventory", "ItemNonInventory", "ItemOtherCharge",
        "ItemPayment", "ItemSalesTax", "ItemSalesTaxGroup", "ItemService", "ItemSubtotal", "Vendor",
    ];

    /// <summary>Merges one list item into another.</summary>
    [McpServerTool(Name = "merge_records", ReadOnly = false, Destructive = true)]
    [Description("Merge a list item into another of the same type; the first is deleted and its transactions move to the second; QuickBooks must be in single-user mode. The user is asked to approve; If the client cannot ask the user, the first call returns a preview and a confirmation_token to pass on a second call.")]
    public Task<string> Merge(
        [Description("Entity type, e.g. Customer, Vendor, Account, ItemService.")] string entity,
        [Description("ListID of the record to merge away (it is deleted).")] string fromId,
        [Description("ListID of the record to keep.")] string toId,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var name = Types.FirstOrDefault(t => t.Equals(entity.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase))
                ?? throw new McpException($"{entity} cannot be merged. Allowed: {string.Join(", ", Types)}.");
            if (fromId == toId) throw new McpException("fromId and toId are the same record.");

            // QuickBooks wants each record's current EditSequence, so read both first.
            var found = Entity.Find(name)!;
            async Task<string> EditSequence(string id) =>
                (string?)(await qb.SendAsync(found.Request, new QueryFilters { Ids = [id] }.ToElements(found, 1), cancellationToken)).Records.FirstOrDefault()?.Element("EditSequence")
                ?? throw new McpException($"No {name} with id {id} was found.");

            var call = new WriteCall(WriteOp.Delete, name, "ListMergeRq",
            [
                new XElement("ListMergeType", name),
                new XElement("MergeFrom", new XElement("ListID", fromId), new XElement("EditSequence", await EditSequence(fromId))),
                new XElement("MergeTo", new XElement("ListID", toId), new XElement("EditSequence", await EditSequence(toId))),
            ]);
            return await executor.ExecuteAsync(call, false, confirmationToken, cancellationToken);
        });
}
