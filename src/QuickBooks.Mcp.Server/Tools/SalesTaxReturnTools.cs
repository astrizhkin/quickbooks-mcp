using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Sales tax return lines.</summary>
[McpServerToolType]
public sealed class SalesTaxReturnTools(IQbClient qb)
{
    /// <summary>Lists the lines of sales tax returns.</summary>
    [McpServerTool(Name = "get_sales_tax_return_lines", ReadOnly = true)]
    [Description("Get the lines of the sales tax return for the given tax agencies. Needs a company file with the sales tax return feature.")]
    public Task<string> GetSalesTaxReturnLines(
        [Description("Full names of the tax agency vendors.")] string[] taxAgencies,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            if (taxAgencies.Length == 0) throw new ArgumentException("Give at least one tax agency.");
            return QbXmlJson.ToJson((await qb.SendAsync("SalesTaxReturnLineQueryRq",
                taxAgencies.Select(a => new XElement("TaxAgencyVendorRef", new XElement("FullName", a))), cancellationToken)).Records);
        });

    /// <summary>Lists the sales tax returns.</summary>
    [McpServerTool(Name = "get_sales_tax_returns", ReadOnly = true)]
    [Description("Get the sales tax returns in the company file. Needs a company file with the sales tax return feature.")]
    public Task<string> GetSalesTaxReturns(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("SalesTaxReturnQueryRq", [], cancellationToken)).Records));
}
