using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and update for sales transactions.</summary>
[McpServerToolType]
public sealed class SalesWriteTools(RecordWriter writer)
{
    /// <summary>The entities these tools accept.</summary>
    public static readonly string[] Entities = ["Invoice", "SalesReceipt", "CreditMemo", "Estimate", "SalesOrder", "ReceivePayment", "ARRefundCreditCard", "Charge", "Deposit"];

    /// <summary>Creates a record.</summary>
    [McpServerTool(Name = "create_sales_transaction", ReadOnly = false, Destructive = false)]
    [Description("Create a sales transaction (invoice, sales receipt, credit memo, estimate, sales order, payment received, refund, charge, deposit). Use describe_write_fields to see the field names; set dryRun to preview the request without sending it.")]
    public Task<string> Create(
        [Description("One of: Invoice,SalesReceipt,CreditMemo,Estimate,SalesOrder,ReceivePayment,ARRefundCreditCard,Charge,Deposit.")] string entity,
        [Description("Fields by qbXML element name, e.g. {\"Name\":\"Acme\",\"BillAddress\":{\"Addr1\":\"1 Main St\"}}. References are {\"FullName\":\"...\"} or {\"ListID\":\"...\"}; repeated elements (lines) are arrays.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        [Description("Optional unique key, e.g. a UUID. Sending the same key and request again creates nothing new and returns the first result, so a retry cannot duplicate.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity, data, dryRun, cancellationToken, idempotencyKey));

    /// <summary>Updates a record.</summary>
    [McpServerTool(Name = "update_sales_transaction", ReadOnly = false, Destructive = true)]
    [Description("Update a sales transaction (invoice, sales receipt, credit memo, estimate, sales order, payment received, refund, charge, deposit); only the fields you give change, and the EditSequence is fetched for you.")]
    public Task<string> Update(
        [Description("One of: Invoice,SalesReceipt,CreditMemo,Estimate,SalesOrder,ReceivePayment,ARRefundCreditCard,Charge,Deposit.")] string entity,
        [Description("ListID or TxnID of the record to change (find it with a search tool).")] string id,
        [Description("The fields to change, by qbXML element name.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.UpdateAsync(Entities, entity, id, data, dryRun, cancellationToken));
}
