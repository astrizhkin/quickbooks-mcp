using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Create and update for lists: names, items, accounts and setup lists.</summary>
[McpServerToolType]
public sealed class ListWriteTools(RecordWriter writer)
{
    /// <summary>The entities these tools accept.</summary>
    public static readonly string[] Entities = ["Account", "Class", "Customer", "Vendor", "Employee", "OtherName", "Lead", "ItemService", "ItemNonInventory", "ItemInventory", "ItemInventoryAssembly", "ItemOtherCharge", "ItemDiscount", "ItemSalesTax", "ItemSalesTaxGroup", "ItemGroup", "ItemSubtotal", "ItemPayment", "ItemFixedAsset", "StandardTerms", "DateDrivenTerms", "PaymentMethod", "SalesTaxCode", "SalesRep", "Vehicle", "BillingRate", "PriceLevel", "WorkersCompCode", "CustomerType", "VendorType", "JobType", "ShipMethod", "CustomerMsg"];

    /// <summary>Creates a record.</summary>
    [McpServerTool(Name = "create_list_item", ReadOnly = false, Destructive = false)]
    [Description("Create a list record (customer, vendor, employee, item, account, class, terms and other setup lists). Use describe_write_fields to see the field names; set dryRun to preview the request without sending it.")]
    public Task<string> Create(
        [Description("One of: Account,Class,Customer,Vendor,Employee,OtherName,Lead,ItemService,ItemNonInventory,ItemInventory,ItemInventoryAssembly,ItemOtherCharge,ItemDiscount,ItemSalesTax,ItemSalesTaxGroup,ItemGroup,ItemSubtotal,ItemPayment,ItemFixedAsset,StandardTerms,DateDrivenTerms,PaymentMethod,SalesTaxCode,SalesRep,Vehicle,BillingRate,PriceLevel,WorkersCompCode,CustomerType,VendorType,JobType,ShipMethod,CustomerMsg.")] string entity,
        [Description("Fields by qbXML element name, e.g. {\"Name\":\"Acme\",\"BillAddress\":{\"Addr1\":\"1 Main St\"}}. References are {\"FullName\":\"...\"} or {\"ListID\":\"...\"}; repeated elements (lines) are arrays.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        [Description("Optional unique key, e.g. a UUID. Sending the same key and request again creates nothing new and returns the first result, so a retry cannot duplicate.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.CreateAsync(Entities, entity, data, dryRun, cancellationToken, idempotencyKey));

    /// <summary>Updates a record.</summary>
    [McpServerTool(Name = "update_list_item", ReadOnly = false, Destructive = true)]
    [Description("Update a list record (customer, vendor, employee, item, account, class, terms and other setup lists). Only the fields you give are changed. The current EditSequence is fetched for you.")]
    public Task<string> Update(
        [Description("One of: Account,Class,Customer,Vendor,Employee,OtherName,Lead,ItemService,ItemNonInventory,ItemInventory,ItemInventoryAssembly,ItemOtherCharge,ItemDiscount,ItemSalesTax,ItemSalesTaxGroup,ItemGroup,ItemSubtotal,ItemPayment,ItemFixedAsset,StandardTerms,DateDrivenTerms,PaymentMethod,SalesTaxCode,SalesRep,Vehicle,BillingRate,PriceLevel,WorkersCompCode,CustomerType,VendorType,JobType,ShipMethod,CustomerMsg.")] string entity,
        [Description("ListID or TxnID of the record to change (find it with a search tool).")] string id,
        [Description("The fields to change, by qbXML element name.")] JsonElement data,
        [Description("true to show the request and send nothing.")] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() => writer.UpdateAsync(Entities, entity, id, data, dryRun, cancellationToken));
}
