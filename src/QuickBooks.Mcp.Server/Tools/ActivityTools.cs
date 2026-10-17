using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Time tracking, vehicle mileage and to-dos.</summary>
[McpServerToolType]
public sealed class ActivityTools(IQbClient qb)
{
    private static readonly string[] Types = ["TimeTracking", "VehicleMileage", "ToDo"];

    /// <summary>Searches time entries, mileage and to-dos.</summary>
    [McpServerTool(Name = "search_activities", ReadOnly = true)]
    [Description("Search time entries (TimeTracking), vehicle mileage (VehicleMileage) or to-dos (ToDo).")]
    public Task<string> SearchActivities(
        [Description("TimeTracking, VehicleMileage or ToDo.")] string type,
        [Description("TimeTracking: employee, vendor or customer full name. ToDo: name contains this text.")] string? name = null,
        [Description("Date on or after (yyyy-MM-dd); not for ToDo.")] string? dateFrom = null,
        [Description("Date on or before (yyyy-MM-dd); not for ToDo.")] string? dateTo = null,
        [Description("TxnID (time, mileage) or ListID (to-do) values.")] string[]? ids = null,
        [Description("Page size (default 50, max 500).")] int? maxResults = null,
        [Description("next_cursor from the previous call.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var isToDo = type.Equals("ToDo", StringComparison.OrdinalIgnoreCase);
        return Search.RunAsync(qb, type, Types,
            new QueryFilters
            {
                Ids = ids,
                EntityName = isToDo ? null : name,
                NameContains = isToDo ? name : null,
                TxnDateFrom = dateFrom,
                TxnDateTo = dateTo,
            },
            maxResults, cursor, cancellationToken);
    }
}
