using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and update for inventory sites and inventory transactions.</summary>
[McpServerToolType]
public sealed class InventoryWriteTools(RecordWriter writer)
{
    /// <summary>The entities these tools accept.</summary>
    public static readonly string[] Entities = ["InventorySite", "InventoryAdjustment", "TransferInventory", "BuildAssembly"];

    /// <summary>Creates a record.</summary>
    [McpServerTool(Name = "create_inventory_record", ReadOnly = false, Destructive = false)]
    [Description("Create an inventory record (inventory site, inventory adjustment, inventory transfer, assembly build). Use describe_write_fields to see the field names; set dryRun to preview the request without sending it.")]
    public Task<string> Create(
        [Description("One of: InventorySite,InventoryAdjustment,TransferInventory,BuildAssembly.")] string entity,
        [Description("Fields by qbXML element name, e.g. {\"Name\":\"Acme\",\"BillAddress\":{\"Addr1\":\"1 Main St\"}}. References are {\"FullName\":\"...\"} or {\"ListID\":\"...\"}; repeated elements (lines) are arrays.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        [Description("Optional unique key, e.g. a UUID. Sending the same key and request again creates nothing new and returns the first result, so a retry cannot duplicate.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity, data, dryRun, cancellationToken, idempotencyKey));

    /// <summary>Updates a record.</summary>
    [McpServerTool(Name = "update_inventory_record", ReadOnly = false, Destructive = true)]
    [Description("Update an inventory record (inventory site, inventory adjustment, inventory transfer, assembly build); only the fields you give change, and the EditSequence is fetched for you.")]
    public Task<string> Update(
        [Description("One of: InventorySite,InventoryAdjustment,TransferInventory,BuildAssembly.")] string entity,
        [Description("ListID or TxnID of the record to change (find it with a search tool).")] string id,
        [Description("The fields to change, by qbXML element name.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.UpdateAsync(Entities, entity, id, data, dryRun, cancellationToken));
}
