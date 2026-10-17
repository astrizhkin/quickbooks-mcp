using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Bills that are due for payment.</summary>
[McpServerToolType]
public sealed class BillToPayTools(IQbClient qb)
{
    /// <summary>Lists unpaid bills and the credits that can be applied to them.</summary>
    [McpServerTool(Name = "get_bills_to_pay", ReadOnly = true)]
    [Description("Get the unpaid bills for one vendor, with the credits available to apply.")]
    public Task<string> GetBillsToPay(
        [Description("Full name of the vendor (or other payee).")] string vendor,
        [Description("Only bills posted to this accounts payable account (full name).")] string? apAccount = null,
        [Description("Bills due on or before this date (yyyy-MM-dd).")] string? dueDate = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            List<XElement> elements = [new("PayeeEntityRef", new XElement("FullName", vendor))];
            if (apAccount is not null) elements.Add(new("APAccountRef", new XElement("FullName", apAccount)));
            if (dueDate is not null)
                elements.Add(new("DueDate", (DateOnly.TryParse(dueDate, CultureInfo.InvariantCulture, out var d)
                    ? d : throw new ArgumentException($"'{dueDate}' is not a date; use yyyy-MM-dd.")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            return QbXmlJson.ToJson((await qb.SendAsync("BillToPayQueryRq", elements, cancellationToken)).Records);
        });
}
