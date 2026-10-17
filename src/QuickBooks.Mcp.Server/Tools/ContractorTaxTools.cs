using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Which accounts feed which boxes of the 1099 forms.</summary>
[McpServerToolType]
public sealed class ContractorTaxTools(IQbClient qb)
{
    /// <summary>Lists the 1099 category-to-account mappings.</summary>
    [McpServerTool(Name = "get_contractor_tax_mappings", ReadOnly = true)]
    [Description("Get which accounts feed which boxes of the 1099 forms.")]
    public Task<string> GetContractorTaxMappings(
        [Description("Form type, e.g. 1099-MISC; default all.")] string? formType = null,
        [Description("Only these boxes, e.g. BOX1, BOX7.")] string[]? boxes = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            List<XElement> elements = [];
            if (formType is not null) elements.Add(new("FormType", formType));
            elements.AddRange((boxes ?? []).Select(b => new XElement("MappingCategory", b.ToUpperInvariant())));
            return QbXmlJson.ToJson((await qb.SendAsync("Form1099CategoryAccountMappingQueryRq", elements, cancellationToken)).Records);
        });
}
