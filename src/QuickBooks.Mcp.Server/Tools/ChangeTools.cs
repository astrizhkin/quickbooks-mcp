using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Incremental sync: what changed or was deleted since a point in time.</summary>
[McpServerToolType]
public sealed class ChangeTools(IQbClient qb)
{
    private const int MaxEntities = 10;

    /// <summary>Returns records modified since a time.</summary>
    [McpServerTool(Name = "get_changes_since", ReadOnly = true)]
    [Description("Get records created or modified since a time, for several entity types at once, plus next_since to use on the next sync. Pair with get_deleted_records to catch removals.")]
    public Task<string> GetChangesSince(
        [Description("Changes on or after this time, yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss (company local time).")] string since,
        [Description("Entity types to check, at most 10, e.g. [\"Customer\", \"Vendor\", \"Item\", \"Invoice\", \"Bill\"].")] string[] entities,
        [Description("Records per entity (default 50, max 500). A next_cursor in an entity result means more; repeat with search tools and the cursor.")] int? maxResults = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            if (entities.Length is 0 or > MaxEntities) throw new ArgumentException($"Name between 1 and {MaxEntities} entity types.");
            var started = DateTime.Now; // taken before the queries so nothing modified meanwhile is missed next time
            var runner = new QueryRunner(qb);
            var results = new JsonArray();
            foreach (var name in entities)
            {
                var entity = Entity.Find(name) ?? throw new ArgumentException($"Unknown entity '{name}'.");
                var filters = new QueryFilters { ModifiedFrom = since, Active = entity.HasActiveStatus ? "all" : null };
                var page = await runner.RunAsync(entity, filters, maxResults, null, cancellationToken);
                page["entity"] = entity.Name;
                results.Add(page);
            }

            return new JsonObject
            {
                ["next_since"] = started.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                ["changes"] = results,
            }.ToJsonString(new() { WriteIndented = true });
        });

    /// <summary>Returns records deleted since a time.</summary>
    [McpServerTool(Name = "get_deleted_records", ReadOnly = true)]
    [Description("Get list items (customers, items...) or transactions deleted since a time. Voided transactions are not deletions; they show up as changes.")]
    public Task<string> GetDeletedRecords(
        [Description("list for list items, transaction for transactions.")] string kind,
        [Description("Deleted on or after this time, yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss.")] string since,
        [Description("Only these types, e.g. [\"Customer\"] or [\"Invoice\", \"Bill\"]; default all.")] string[]? types = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var schema = WriteSchema.Instance;
            var (request, typeElement, valid) = kind.ToLowerInvariant() switch
            {
                "list" => ("ListDeletedQueryRq", "ListDelType", schema.ListDelTypes),
                "transaction" => ("TxnDeletedQueryRq", "TxnDelType", schema.TxnDelTypes),
                _ => throw new ArgumentException("kind must be list or transaction."),
            };
            var chosen = types is { Length: > 0 }
                ? types.Select(t => valid.FirstOrDefault(v => v.Equals(t, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"'{t}' is not a {kind} type. Use: {string.Join(", ", valid)}.")).ToList()
                : valid.ToList();

            var elements = chosen.Select(t => new XElement(typeElement, t)).ToList();
            elements.Add(new XElement("DeletedDateRangeFilter", new XElement("FromDeletedDate", QueryFilters.Timestamp(since))));
            return QbXmlJson.ToJson((await qb.SendAsync(request, elements, cancellationToken)).Records);
        });
}
