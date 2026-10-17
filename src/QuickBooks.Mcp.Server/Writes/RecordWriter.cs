using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ModelContextProtocol;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>Create and update for any entity in a given set; the write tool classes are thin wrappers over this.</summary>
public sealed class RecordWriter(IQbClient qb, WriteExecutor executor)
{
    private static readonly WriteSchema Schema = WriteSchema.Instance;

    /// <summary>Creates a record (<c>{Entity}AddRq</c>).</summary>
    public Task<string> CreateAsync(
        IReadOnlyCollection<string> allowed, string entity, JsonElement data, bool dryRun, CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        var name = Canonical(allowed, entity);
        var element = Schema.Build(name, "Add", AsObject(data));
        if (name == "JournalEntry") EnsureBalanced(element);
        return executor.ExecuteAsync(new WriteCall(WriteOp.Create, name, name + "AddRq", [element]), dryRun, null, cancellationToken, idempotencyKey);
    }

    /// <summary>
    /// Updates a record (<c>{Entity}ModRq</c>). QuickBooks needs the record's current EditSequence, so the
    /// record is read first; only the fields in <paramref name="data"/> are changed.
    /// </summary>
    public async Task<string> UpdateAsync(IReadOnlyCollection<string> allowed, string entity, string id, JsonElement data, bool dryRun, CancellationToken cancellationToken)
    {
        var name = Canonical(allowed, entity);
        var found = Entity.Find(name) ?? throw new McpException($"{name} cannot be updated.");
        var fields = AsObject(data);

        var current = (await qb.SendAsync(found.Request, new QueryFilters { Ids = [id] }.ToElements(found, 1), cancellationToken)).Records.FirstOrDefault()
            ?? throw new McpException($"No {name} with id {id} was found.");
        var editSequence = (string?)current.Element("EditSequence") ?? throw new McpException($"{name} has no EditSequence and cannot be updated.");

        fields[found.IsTransaction ? "TxnID" : "ListID"] = id;
        fields["EditSequence"] = editSequence;
        var element = Schema.Build(name, "Mod", fields);
        return await executor.ExecuteAsync(new WriteCall(WriteOp.Update, name, name + "ModRq", [element]), dryRun, null, cancellationToken);
    }

    // Catches the most common model mistake before QuickBooks does: debits that do not equal credits.
    private static void EnsureBalanced(XElement entry)
    {
        decimal Total(string line) => entry.Elements(line).Sum(l => decimal.TryParse((string?)l.Element("Amount"), System.Globalization.CultureInfo.InvariantCulture, out var a) ? a : 0m);
        var (debits, credits) = (Total("JournalDebitLine"), Total("JournalCreditLine"));
        if (debits != credits)
            throw new McpException($"The journal entry is not balanced: debits {debits} but credits {credits}. Nothing was sent.");
    }

    private static string Canonical(IReadOnlyCollection<string> allowed, string entity) =>
        allowed.FirstOrDefault(a => a.Equals(entity.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase))
        ?? throw new McpException($"Unknown entity '{entity}'. Use one of: {string.Join(", ", allowed)}.");

    private static JsonObject AsObject(JsonElement data) =>
        JsonNode.Parse(data.GetRawText()) as JsonObject ?? throw new McpException("data must be a JSON object of field names and values.");
}
