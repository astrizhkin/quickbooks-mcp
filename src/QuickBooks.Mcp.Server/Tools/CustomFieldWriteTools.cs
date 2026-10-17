using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and remove custom fields, and set their values on records.</summary>
[McpServerToolType]
public sealed class CustomFieldWriteTools(WriteExecutor executor)
{
    private static readonly string[] FieldTypes = ["AMTTYPE", "DATETIMETYPE", "INTTYPE", "PERCENTTYPE", "PRICETYPE", "QUANTYPE", "STR1024TYPE", "STR255TYPE"];

    /// <summary>Defines a custom field.</summary>
    [McpServerTool(Name = "create_custom_field_definition", ReadOnly = false, Destructive = false)]
    [Description("Define a new custom field (data extension) and the kinds of record it applies to.")]
    public Task<string> CreateDefinition(
        [Description("Field name, unique per kind of record.")] string name,
        [Description("AMTTYPE (amount), DATETIMETYPE, INTTYPE, PERCENTTYPE, PRICETYPE, QUANTYPE, STR1024TYPE or STR255TYPE (text).")] string type,
        [Description("Kinds of record it applies to, e.g. [\"Customer\", \"Invoice\"].")] string[] appliesTo,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() =>
        {
            var fieldType = FieldTypes.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"type must be one of {string.Join(", ", FieldTypes)}.");
            if (appliesTo.Length == 0) throw new ArgumentException("appliesTo needs at least one kind of record.");
            var element = new XElement("DataExtDefAdd", new XElement("OwnerID", "0"), new XElement("DataExtName", name),
                new XElement("DataExtType", fieldType), appliesTo.Select(a => new XElement("AssignToObject", a)));
            return executor.ExecuteAsync(new WriteCall(WriteOp.Create, "DataExtDef", "DataExtDefAddRq", [element]), dryRun, null, cancellationToken);
        });

    /// <summary>Removes a custom field definition.</summary>
    [McpServerTool(Name = "delete_custom_field_definition", ReadOnly = false, Destructive = true)]
    [Description("Delete a custom field definition and every value stored in it. The first call changes nothing and returns a preview and a confirmation_token; call again with the token to confirm.")]
    public Task<string> DeleteDefinition(
        [Description("Field name.")] string name,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => executor.ExecuteAsync(
            new WriteCall(WriteOp.Delete, "DataExtDef", "DataExtDefDelRq", [new XElement("OwnerID", "0"), new XElement("DataExtName", name)]),
            false, confirmationToken, cancellationToken));

    /// <summary>Sets a custom field value on a record.</summary>
    [McpServerTool(Name = "update_custom_field_value", ReadOnly = false, Destructive = true)]
    [Description("Set the value of a custom field on a record (adds it the first time, changes it afterwards).")]
    public Task<string> UpdateValue(
        [Description("Kind of record, e.g. Customer, Vendor, Item, Invoice.")] string recordType,
        [Description("ListID (lists) or TxnID (transactions) of the record.")] string id,
        [Description("Custom field name.")] string field,
        [Description("The value.")] string value,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var (kind, owner) = Target(recordType, id, field);
            XElement Build(string name) => new(name, owner(), new XElement("DataExtValue", value));
            try
            {
                return await executor.ExecuteAsync(new WriteCall(WriteOp.Update, kind, "DataExtAddRq", [Build("DataExtAdd")]), dryRun, null, cancellationToken);
            }
            catch (QbException)
            {
                // The value already exists, so change it instead of adding it.
                return await executor.ExecuteAsync(new WriteCall(WriteOp.Update, kind, "DataExtModRq", [Build("DataExtMod")]), dryRun, null, cancellationToken);
            }
        });

    /// <summary>Clears a custom field value.</summary>
    [McpServerTool(Name = "delete_custom_field_value", ReadOnly = false, Destructive = true)]
    [Description("Remove the value of a custom field from a record. The first call changes nothing and returns a preview and a confirmation_token; call again with the token to confirm.")]
    public Task<string> DeleteValue(
        [Description("Kind of record, e.g. Customer, Vendor, Item, Invoice.")] string recordType,
        [Description("ListID (lists) or TxnID (transactions) of the record.")] string id,
        [Description("Custom field name.")] string field,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() =>
        {
            var (kind, owner) = Target(recordType, id, field);
            return executor.ExecuteAsync(new WriteCall(WriteOp.Delete, kind, "DataExtDelRq", [.. owner()]), false, confirmationToken, cancellationToken);
        });

    // DataExt addresses a record by list type + ListObjRef, or by transaction type + TxnID, in this order.
    private static (string Kind, Func<IEnumerable<XElement>> Owner) Target(string recordType, string id, string field)
    {
        var entity = Entity.Find(recordType) ?? throw new McpException($"Unknown record type '{recordType}'.");
        var kind = entity.Name.StartsWith("Item", StringComparison.Ordinal) && entity.Name != "ItemReceipt" ? "Item" : entity.Name;
        return (kind, () => entity.IsTransaction
            ? [new("OwnerID", "0"), new("DataExtName", field), new("TxnDataExtType", kind), new("TxnID", id)]
            : [new("OwnerID", "0"), new("DataExtName", field), new("ListDataExtType", kind), new("ListObjRef", new XElement("ListID", id))]);
    }
}
