using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Deleting and voiding, the most dangerous writes: always a preview first, then a confirmation token.</summary>
[McpServerToolType]
public sealed class DeleteTools(WriteExecutor executor)
{
    private static readonly WriteSchema Schema = WriteSchema.Instance;

    /// <summary>Deletes a list item or a transaction.</summary>
    [McpServerTool(Name = "delete_record", ReadOnly = false, Destructive = true)]
    [Description("Permanently delete a list item (customer, vendor, item...) or a transaction. If the client can ask the user, they are asked and the change is made once they approve. Otherwise the first call changes nothing and returns a preview and a confirmation_token; show it to the user, and only if they agree call again with the token. Prefer void_transaction for transactions that can be voided.")]
    public Task<string> Delete(
        [Description("Entity type, e.g. Customer, Vendor, ItemService, Invoice, Bill, Check.")] string entity,
        [Description("ListID or TxnID of the record.")] string id,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() =>
        {
            var (name, isList) = Resolve(entity, Schema.ListDelTypes, Schema.TxnDelTypes, "deleted");
            WriteCall call = isList
                ? new(WriteOp.Delete, name, "ListDelRq", [new XElement("ListDelType", name), new XElement("ListID", id)])
                : new(WriteOp.Delete, name, "TxnDelRq", [new XElement("TxnDelType", name), new XElement("TxnID", id)]);
            return executor.ExecuteAsync(call, false, confirmationToken, cancellationToken);
        });

    /// <summary>Voids a transaction.</summary>
    [McpServerTool(Name = "void_transaction", ReadOnly = false, Destructive = true)]
    [Description("Void a transaction: it stays in the books with a zero amount. If the client can ask the user, they are asked and the change is made once they approve. Otherwise the first call changes nothing and returns a preview and a confirmation_token; show it to the user, and only if they agree call again with the token.")]
    public Task<string> Void(
        [Description("Transaction type, e.g. Invoice, Bill, Check, CreditMemo, SalesReceipt, JournalEntry.")] string entity,
        [Description("TxnID of the transaction.")] string id,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() =>
        {
            var (name, _) = Resolve(entity, [], Schema.TxnVoidTypes, "voided");
            var call = new WriteCall(WriteOp.Delete, name, "TxnVoidRq", [new XElement("TxnVoidType", name), new XElement("TxnID", id)]);
            return executor.ExecuteAsync(call, false, confirmationToken, cancellationToken);
        });

    private static (string Name, bool IsList) Resolve(string entity, IReadOnlyList<string> lists, IReadOnlyList<string> transactions, string verb)
    {
        var wanted = entity.Replace(" ", "").Replace("_", "");
        var list = lists.FirstOrDefault(t => t.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        if (list is not null) return (list, true);
        var txn = transactions.FirstOrDefault(t => t.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        if (txn is not null) return (txn, false);
        throw new McpException($"{entity} cannot be {verb}. Allowed: {string.Join(", ", lists.Concat(transactions))}.");
    }
}
