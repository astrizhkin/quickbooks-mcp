using ModelContextProtocol;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>What every <c>search_*</c> tool does once it has its filters: one query path for all entities.</summary>
internal static class Search
{
    /// <summary>
    /// Queries <paramref name="entityName"/>, which must be one of <paramref name="allowed"/>
    /// (null means any), and returns the page as JSON.
    /// </summary>
    public static Task<string> RunAsync(
        IQbClient qb,
        string entityName,
        IReadOnlyCollection<string>? allowed,
        QueryFilters filters,
        int? maxResults,
        string? cursor,
        CancellationToken cancellationToken) =>
        ToolErrors.RunAsync(async () =>
        {
            var entity = Entity.Find(entityName);
            if (entity is null || (allowed is not null && !allowed.Contains(entity.Name, StringComparer.OrdinalIgnoreCase)))
                throw new McpException($"Unknown value '{entityName}'. Use one of: {string.Join(", ", allowed ?? Entity.All.Select(e => e.Name).ToList())}.");

            var result = await new QueryRunner(qb).RunAsync(entity, filters, maxResults, cursor, cancellationToken);
            return result.ToJsonString(new() { WriteIndented = true });
        });
}
