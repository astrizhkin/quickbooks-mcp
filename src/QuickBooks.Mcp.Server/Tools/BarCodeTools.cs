using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Bar code lookups.</summary>
[McpServerToolType]
public sealed class BarCodeTools(IQbClient qb)
{
    /// <summary>Finds the items that carry given bar code values.</summary>
    [McpServerTool(Name = "get_barcodes", ReadOnly = true)]
    [Description("Look up bar codes and the items they belong to. Give bar code values, or none to list the bar codes in the company file.")]
    public Task<string> GetBarCodes(
        [Description("Bar code values to look up.")] string[]? values = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("BarCodeQueryRq",
                (values ?? []).Select(v => new XElement("BarCodeValue", v)), cancellationToken)).Records));
}
