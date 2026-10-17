using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Company information tools.</summary>
[McpServerToolType]
public sealed class CompanyTools(IQbClient qb)
{
    /// <summary>Returns the open company file's information.</summary>
    [McpServerTool(Name = "get_company", ReadOnly = true)]
    [Description("Get the company name, address and other details of the open QuickBooks company file.")]
    public Task<string> GetCompany(CancellationToken cancellationToken) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("CompanyQueryRq", cancellationToken: cancellationToken)).Records));
}
