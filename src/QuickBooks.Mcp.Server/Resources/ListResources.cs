using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;

namespace QuickBooks.Mcp.Server.Resources;

/// <summary>Stable lists a client can attach as context without calling a tool.</summary>
[McpServerResourceType]
public sealed class ListResources(IQbClient qb)
{
    /// <summary>The active chart of accounts.</summary>
    [McpServerResource(UriTemplate = "quickbooks://accounts", Name = "Chart of accounts", MimeType = "application/json")]
    [Description("The active chart of accounts (name, type, balance).")]
    public Task<string> Accounts(CancellationToken cancellationToken) =>
        Search.RunAsync(qb, "Account", null, new QueryFilters(), QueryRunner.MaxPageSize, null, cancellationToken);

    /// <summary>The active items.</summary>
    [McpServerResource(UriTemplate = "quickbooks://items", Name = "Items", MimeType = "application/json")]
    [Description("The active items (products and services).")]
    public Task<string> Items(CancellationToken cancellationToken) =>
        Search.RunAsync(qb, "Item", null, new QueryFilters(), QueryRunner.MaxPageSize, null, cancellationToken);
}
