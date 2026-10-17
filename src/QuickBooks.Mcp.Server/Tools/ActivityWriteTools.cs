using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and update for time entries, vehicle mileage and to-dos.</summary>
[McpServerToolType]
public sealed class ActivityWriteTools(RecordWriter writer)
{
    /// <summary>The entities these tools accept.</summary>
    public static readonly string[] Entities = ["TimeTracking", "VehicleMileage", "ToDo"];

    /// <summary>Creates a record.</summary>
    [McpServerTool(Name = "create_activity", ReadOnly = false, Destructive = false)]
    [Description("Create a time entry, a vehicle mileage record or a to-do. Use describe_write_fields to see the field names; set dryRun to preview the request without sending it.")]
    public Task<string> Create(
        [Description("One of: TimeTracking,VehicleMileage,ToDo.")] string entity,
        [Description("Fields by qbXML element name, e.g. {\"Name\":\"Acme\",\"BillAddress\":{\"Addr1\":\"1 Main St\"}}. References are {\"FullName\":\"...\"} or {\"ListID\":\"...\"}; repeated elements (lines) are arrays.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        [Description("Optional unique key, e.g. a UUID. Sending the same key and request again creates nothing new and returns the first result, so a retry cannot duplicate.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity, data, dryRun, cancellationToken, idempotencyKey));

    /// <summary>Updates a record.</summary>
    [McpServerTool(Name = "update_activity", ReadOnly = false, Destructive = true)]
    [Description("Update a time entry, a vehicle mileage record or a to-do. Only the fields you give are changed. The current EditSequence is fetched for you.")]
    public Task<string> Update(
        [Description("One of: TimeTracking,VehicleMileage,ToDo.")] string entity,
        [Description("ListID or TxnID of the record to change (find it with a search tool).")] string id,
        [Description("The fields to change, by qbXML element name.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.UpdateAsync(Entities, entity, id, data, dryRun, cancellationToken));
}
