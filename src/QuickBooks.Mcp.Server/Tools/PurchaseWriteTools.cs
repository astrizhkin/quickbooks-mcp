using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and update for purchase and banking transactions.</summary>
[McpServerToolType]
public sealed class PurchaseWriteTools(RecordWriter writer)
{
    /// <summary>The entities these tools accept.</summary>
    public static readonly string[] Entities = ["Bill", "BillPaymentCheck", "BillPaymentCreditCard", "PurchaseOrder", "ItemReceipt", "VendorCredit", "Check", "CreditCardCharge", "CreditCardCredit", "SalesTaxPaymentCheck", "Transfer", "JournalEntry"];

    /// <summary>Creates a record.</summary>
    [McpServerTool(Name = "create_purchase_transaction", ReadOnly = false, Destructive = false)]
    [Description("Create a purchase or banking transaction (bill, bill payment, purchase order, item receipt, vendor credit, check, credit card charge or credit, sales tax payment, transfer, journal entry). Use describe_write_fields to see the field names; set dryRun to preview the request without sending it.")]
    public Task<string> Create(
        [Description("One of: Bill,BillPaymentCheck,BillPaymentCreditCard,PurchaseOrder,ItemReceipt,VendorCredit,Check,CreditCardCharge,CreditCardCredit,SalesTaxPaymentCheck,Transfer,JournalEntry.")] string entity,
        [Description("Fields by qbXML element name, e.g. {\"Name\":\"Acme\",\"BillAddress\":{\"Addr1\":\"1 Main St\"}}. References are {\"FullName\":\"...\"} or {\"ListID\":\"...\"}; repeated elements (lines) are arrays.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        [Description("Optional unique key, e.g. a UUID. Sending the same key and request again creates nothing new and returns the first result, so a retry cannot duplicate.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity, data, dryRun, cancellationToken, idempotencyKey));

    /// <summary>Updates a record.</summary>
    [McpServerTool(Name = "update_purchase_transaction", ReadOnly = false, Destructive = true)]
    [Description("Update a purchase or banking transaction (bill, bill payment, purchase order, item receipt, vendor credit, check, credit card charge or credit, sales tax payment, transfer, journal entry); only the fields you give change, and the EditSequence is fetched for you.")]
    public Task<string> Update(
        [Description("One of: Bill,BillPaymentCheck,BillPaymentCreditCard,PurchaseOrder,ItemReceipt,VendorCredit,Check,CreditCardCharge,CreditCardCredit,SalesTaxPaymentCheck,Transfer,JournalEntry.")] string entity,
        [Description("ListID or TxnID of the record to change (find it with a search tool).")] string id,
        [Description("The fields to change, by qbXML element name.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.UpdateAsync(Entities, entity, id, data, dryRun, cancellationToken));
}
