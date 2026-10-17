using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>The QuickBooks product that is running.</summary>
[McpServerToolType]
public sealed class HostTools(IQbClient qb)
{
    /// <summary>Gets the product name, version and supported qbXML versions.</summary>
    [McpServerTool(Name = "get_host_info", ReadOnly = true)]
    [Description("Get the QuickBooks product, version and the qbXML versions it supports.")]
    public Task<string> GetHostInfo(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("HostQueryRq", [], cancellationToken)).Records));
}
