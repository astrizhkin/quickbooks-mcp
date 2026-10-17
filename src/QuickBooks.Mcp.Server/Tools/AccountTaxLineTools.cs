using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Tax form lines that accounts map to.</summary>
[McpServerToolType]
public sealed class AccountTaxLineTools(IQbClient qb)
{
    /// <summary>Lists the tax form lines available for mapping accounts.</summary>
    [McpServerTool(Name = "get_account_tax_lines", ReadOnly = true)]
    [Description("Get the income tax form lines that accounts can be assigned to, as set up in the company file.")]
    public Task<string> GetAccountTaxLines(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("AccountTaxLineInfoQueryRq", [], cancellationToken)).Records));
}
