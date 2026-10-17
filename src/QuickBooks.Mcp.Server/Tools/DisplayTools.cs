using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Shows a record in the QuickBooks window so the user can see what is being talked about.</summary>
[McpServerToolType]
public sealed class DisplayTools(IQbClient qb)
{
    private static readonly WriteSchema Schema = WriteSchema.Instance;

    /// <summary>Opens a customer, vendor, employee, item, account or transaction in QuickBooks.</summary>
    [McpServerTool(Name = "open_in_quickbooks", ReadOnly = true)]
    [Description("Open a record in the QuickBooks window for the user to look at; changes nothing, and QuickBooks must be open.")]
    public Task<string> OpenInQuickBooks(
        [Description("Kind of record: Customer, Vendor, Employee, Account, Item (any item type), OtherName, or a transaction such as Invoice, Bill, Check, Estimate, SalesOrder, PurchaseOrder, JournalEntry.")] string recordType,
        [Description("ListID (lists) or TxnID (transactions), from a search tool.")] string id,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var entity = Entity.Find(recordType) ?? throw new ArgumentException($"Unknown record type '{recordType}'.");
            // Every item type opens through the generic Item window.
            var kind = entity.Name.StartsWith("Item", StringComparison.Ordinal) && entity.Name != "ItemReceipt" ? "Item" : entity.Name;

            var (request, typeElement, idElement, allowed) = entity.IsTransaction
                ? ("TxnDisplayModRq", "TxnDisplayModType", "TxnID", Schema.TxnDisplayTypes)
                : ("ListDisplayModRq", "ListDisplayModType", "ListID", Schema.ListDisplayTypes);
            if (!allowed.Contains(kind, StringComparer.Ordinal))
                throw new McpException($"{recordType} cannot be opened in QuickBooks. Open one of: {string.Join(", ", Schema.ListDisplayTypes.Concat(Schema.TxnDisplayTypes))}.");

            try
            {
                await qb.SendAsync(request, [new XElement(typeElement, kind), new XElement(idElement, id)], cancellationToken);
            }
            catch (Exception ex) when (ex is QbException or COMException)
            {
                throw new McpException($"QuickBooks could not show the record. It must be open on this computer with the company file loaded. ({ex.Message})");
            }

            return $"Opened the {kind} in the QuickBooks window.";
        });
}
