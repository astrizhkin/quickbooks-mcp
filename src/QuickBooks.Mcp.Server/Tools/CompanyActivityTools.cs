using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>When the company file last changed.</summary>
[McpServerToolType]
public sealed class CompanyActivityTools(IQbClient qb)
{
    /// <summary>Gets the time of the last change to the company file.</summary>
    [McpServerTool(Name = "get_company_activity", ReadOnly = true)]
    [Description("Get when the company file was last changed. Cheap to call, so use it to check whether anything changed before re-reading data.")]
    public Task<string> GetCompanyActivity(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("CompanyActivityQueryRq", [], cancellationToken)).Records));
}
