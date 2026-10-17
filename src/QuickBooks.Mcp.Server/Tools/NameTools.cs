using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Customers, vendors, employees, other names and leads.</summary>
[McpServerToolType]
public sealed class NameTools(IQbClient qb)
{
    /// <summary>Searches customers and jobs.</summary>
    [McpServerTool(Name = "search_customers", ReadOnly = true)]
    [Description("Search customers and jobs by name. Jobs are customers whose FullName is Customer:Job.")]
    public Task<string> SearchCustomers(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("Exact full names, like Smith:Kitchen Remodel.")] string[]? fullNames = null,
        [Description("ListID values.")] string[]? ids = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Modified on or after this date (yyyy-MM-dd).")] string? modifiedFrom = null,
        [Description("Return only these fields, e.g. Name, Balance.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default) =>
        Names("Customer", nameContains, fullNames, ids, active, modifiedFrom, fields, includeCustomFields, maxResults, cursor, cancellationToken);

    /// <summary>Searches vendors.</summary>
    [McpServerTool(Name = "search_vendors", ReadOnly = true)]
    [Description("Search vendors by name.")]
    public Task<string> SearchVendors(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("Exact full names.")] string[]? fullNames = null,
        [Description("ListID values.")] string[]? ids = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Modified on or after this date (yyyy-MM-dd).")] string? modifiedFrom = null,
        [Description("Return only these fields.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default) =>
        Names("Vendor", nameContains, fullNames, ids, active, modifiedFrom, fields, includeCustomFields, maxResults, cursor, cancellationToken);

    /// <summary>Searches employees.</summary>
    [McpServerTool(Name = "search_employees", ReadOnly = true)]
    [Description("Search employees by name. Payroll fields are included only if the QuickBooks user may see them.")]
    public Task<string> SearchEmployees(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("Exact full names.")] string[]? fullNames = null,
        [Description("ListID values.")] string[]? ids = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Modified on or after this date (yyyy-MM-dd).")] string? modifiedFrom = null,
        [Description("Return only these fields.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        [Description("Also return custom fields (DataExt).")] bool includeCustomFields = false,
        CancellationToken cancellationToken = default) =>
        Names("Employee", nameContains, fullNames, ids, active, modifiedFrom, fields, includeCustomFields, maxResults, cursor, cancellationToken);

    /// <summary>Searches other names and leads.</summary>
    [McpServerTool(Name = "search_other_names", ReadOnly = true)]
    [Description("Search other names (people paid who are not vendors or employees) or leads.")]
    public Task<string> SearchOtherNames(
        [Description("OtherName (default) or Lead.")] string kind = "OtherName",
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Return only these fields.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, kind, ["OtherName", "Lead"],
            new QueryFilters { NameContains = nameContains, Active = active, Fields = fields },
            maxResults, cursor, cancellationToken);

    /// <summary>Searches all name lists at once.</summary>
    [McpServerTool(Name = "search_names", ReadOnly = true)]
    [Description("Search customers, vendors, employees and other names at once by name. Use when you do not know which list a name is in.")]
    public Task<string> SearchNames(
        [Description("Name contains this text.")] string? nameContains = null,
        [Description("active (default), inactive or all.")] string? active = null,
        [Description("Return only these fields.")] string[]? fields = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default) =>
        Search.RunAsync(qb, "Entity", ["Entity"],
            new QueryFilters { NameContains = nameContains, Active = active, Fields = fields },
            maxResults, cursor, cancellationToken);

    private Task<string> Names(
        string entity, string? nameContains, string[]? fullNames, string[]? ids, string? active,
        string? modifiedFrom, string[]? fields, bool includeCustomFields, int? maxResults, string? cursor, CancellationToken cancellationToken) =>
        Search.RunAsync(qb, entity, [entity],
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
