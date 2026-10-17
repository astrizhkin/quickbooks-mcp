using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Creates many records in one call.</summary>
[McpServerToolType]
public sealed class BatchWriteTools(RecordWriter writer)
{
    private const int MaxItems = 50;
    private static readonly string[] Entities = [.. ListWriteTools.Entities, .. SalesWriteTools.Entities, .. PurchaseWriteTools.Entities, .. InventoryWriteTools.Entities];

    /// <summary>Creates several records, reporting each outcome.</summary>
    [McpServerTool(Name = "create_records", ReadOnly = false, Destructive = false)]
    [Description("Create up to 50 records in one call. Each item is {entity, data, idempotencyKey?}. Items run in order; by default a failed item does not stop the rest, and every item gets its own result. Give each item an idempotencyKey so a retry of the whole call cannot duplicate.")]
    public async Task<string> CreateRecords(
        [Description("Array of {\"entity\": \"Invoice\", \"data\": {...}, \"idempotencyKey\": \"...\"}.")] JsonElement items,
        [Description("true to preview every item and send nothing.")] bool dryRun = false,
        [Description("true to stop at the first failure.")] bool stopOnError = false,
        CancellationToken cancellationToken = default)
    {
        if (items.ValueKind != JsonValueKind.Array) throw new McpException("items must be an array.");
        if (items.GetArrayLength() is 0 or > MaxItems) throw new McpException($"Send between 1 and {MaxItems} items.");

        var results = new JsonArray();
        var index = 0;
        foreach (var item in items.EnumerateArray())
        {
            JsonObject result = new() { ["index"] = index++ };
            try
            {
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("entity", out var entity) || !item.TryGetProperty("data", out var data))
                    throw new McpException("Each item needs entity and data.");
                var key = item.TryGetProperty("idempotencyKey", out var k) ? k.GetString() : null;
                var outcome = await ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity.GetString() ?? "", data, dryRun, cancellationToken, key));
                result["ok"] = true;
                result["result"] = JsonNode.Parse(outcome);
            }
            catch (McpException ex)
            {
                result["ok"] = false;
                result["error"] = ex.Message;
                results.Add(result);
                if (stopOnError) break;
                continue;
            }

            results.Add(result);
        }

        return new JsonObject
        {
            ["succeeded"] = results.Count(r => (bool)r!["ok"]!),
            ["failed"] = results.Count(r => !(bool)r!["ok"]!),
            ["results"] = results,
        }.ToJsonString(new() { WriteIndented = true });
    }
}
