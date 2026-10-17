using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Custom field definitions and sales tax owed.</summary>
[McpServerToolType]
public sealed class CustomFieldTools(IQbClient qb)
{
    /// <summary>Lists custom field definitions.</summary>
    [McpServerTool(Name = "get_custom_field_definitions", ReadOnly = true)]
    [Description("List the custom fields (data extensions) defined in the company file and which kinds of record they apply to. To see their values, search with includeCustomFields=true.")]
    public Task<string> GetCustomFieldDefinitions(
        [Description("Only fields assigned to this kind of record, e.g. Customer, Vendor, Item, Invoice.")] string? appliesTo = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("DataExtDefQueryRq",
                [appliesTo is null ? new XElement("OwnerID", "0") : new XElement("AssignToObject", appliesTo)], cancellationToken)).Records));

    /// <summary>Reports sales tax owed.</summary>
    [McpServerTool(Name = "get_sales_tax_payable", ReadOnly = true)]
    [Description("Get the sales tax payable to each tax agency as of a date.")]
    public Task<string> GetSalesTaxPayable(
        [Description("As of this date (yyyy-MM-dd); default today.")] string? asOfDate = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var date = asOfDate is null
                ? DateOnly.FromDateTime(DateTime.Today)
                : DateOnly.TryParse(asOfDate, CultureInfo.InvariantCulture, out var d) ? d : throw new ArgumentException($"'{asOfDate}' is not a date; use yyyy-MM-dd.");
            return QbXmlJson.ToJson((await qb.SendAsync("SalesTaxPayableQueryRq",
                [new XElement("AsOfDate", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))], cancellationToken)).Records);
        });
}
