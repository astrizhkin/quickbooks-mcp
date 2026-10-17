using System.Globalization;
using System.Text.Json.Nodes;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Query;

/// <summary>Runs a query against one entity and pages through the results.</summary>
public sealed class QueryRunner(IQbClient qb)
{
    /// <summary>Records returned per page when the caller does not say.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>Largest page a caller can ask for, to protect the model's context.</summary>
    public const int MaxPageSize = 500;

    /// <summary>
    /// Returns <c>{ count, records, next_cursor }</c>. The cursor is the number of records already
    /// returned: QuickBooks iterators do not survive our short per-call sessions, so the next page
    /// re-queries and skips (shortcut: cost grows with depth, fine for page-by-page browsing).
    /// </summary>
    public async Task<JsonObject> RunAsync(
        Entity entity, QueryFilters filters, int? maxResults, string? cursor, CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(maxResults ?? DefaultPageSize, 1, MaxPageSize);
        var skip = 0;
        if (cursor is not null && !int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out skip))
            throw new ArgumentException("cursor must be the next_cursor value from a previous call.");

        // One extra record tells us whether another page exists.
        var response = await qb.SendAsync(entity.Request, filters.ToElements(entity, skip + size + 1), cancellationToken);
        var page = response.Records.Skip(skip).ToList();

        return new JsonObject
        {
            ["count"] = Math.Min(page.Count, size),
            ["records"] = new JsonArray(page.Take(size).Select(QbXmlJson.ToNode).ToArray()),
            ["next_cursor"] = page.Count > size ? (skip + size).ToString(CultureInfo.InvariantCulture) : null,
        };
    }
}
