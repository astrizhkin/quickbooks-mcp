using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Helps the model build valid data for the write tools. Registered only when writes are enabled.</summary>
[McpServerToolType]
public sealed class WriteSchemaTools
{
    /// <summary>Describes the fields of a create or update request.</summary>
    [McpServerTool(Name = "describe_write_fields", ReadOnly = true)]
    [Description("List the field names accepted by create or update for an entity, as a tree. Empty text is a plain value, [..] is a repeatable element, nested objects are groups such as addresses and lines, and a value like 12.0+ means that field needs qbXML 12.0 or a newer QuickBooks.")]
    public Task<string> DescribeWriteFields(
        [Description("Entity name, e.g. Customer, Invoice, Bill, JournalEntry.")] string entity,
        [Description("create (default) or update.")] string operation = "create") =>
        ToolErrors.RunAsync(() => Task.FromResult(
            WriteSchema.Instance.Describe(entity.Replace(" ", ""), operation.ToLowerInvariant() switch
            {
                "create" => "Add",
                "update" => "Mod",
                _ => throw new ArgumentException("operation must be create or update."),
            }).ToJsonString(new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })));
}
