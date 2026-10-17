using System.Globalization;
using System.Runtime.InteropServices;
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

    /// <summary>Rough cap on the serialized size of one page; longer pages are cut and continue at next_cursor.</summary>
    public const int MaxResponseChars = 100_000;

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
        var wanted = page.Take(size).Select(QbXmlJson.ToNode).ToList();

        // Hard size cap: keep whole records until the page is big enough, but always at least one.
        var kept = new JsonArray();
        var chars = 0;
        foreach (var record in wanted)
        {
            chars += record.ToJsonString().Length;
            if (kept.Count > 0 && chars > MaxResponseChars) break;
            kept.Add(record);
        }

        var result = new JsonObject
        {
            ["count"] = kept.Count,
            ["records"] = kept,
            ["next_cursor"] = page.Count > kept.Count ? (skip + kept.Count).ToString(CultureInfo.InvariantCulture) : null,
        };

        // A CurrencyRef only appears on foreign-currency records, so this costs nothing for single-currency companies.
        if (page.Take(kept.Count).Any(r => r.Descendants("CurrencyRef").Any()))
        {
            string? home = null;
            try
            {
                home = await CurrencyContext.For(qb).HomeCurrencyAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is COMException or QbException or InvalidOperationException)
            {
                // the amounts are still correct; only the home currency name is missing from the note
            }

            result["currency"] = new JsonObject { ["home"] = home, ["note"] = CurrencyContext.Note(home) };
        }

        return result;
    }
}
