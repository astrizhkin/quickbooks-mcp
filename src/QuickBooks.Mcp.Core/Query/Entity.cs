namespace QuickBooks.Mcp.Core.Query;

/// <summary>A QuickBooks entity that can be queried with <c>{Name}QueryRq</c>.</summary>
/// <param name="Name">Entity name as used in qbXML and in tool arguments, e.g. "Customer".</param>
/// <param name="IsTransaction">Transactions are keyed by TxnID and filtered by date, entity and account; list items by ListID and name.</param>
public sealed record Entity(string Name, bool IsTransaction)
{
    /// <summary>The qbXML request element, e.g. <c>CustomerQueryRq</c>.</summary>
    public string Request => Name + "QueryRq";

    /// <summary>The single table of queryable entities; per-entity tools are built from it.</summary>
    public static readonly IReadOnlyList<Entity> All =
    [
        .. new[]
        {
            "Account", "BillingRate", "Class", "Currency", "Customer", "CustomerMsg", "CustomerType", "DateDrivenTerms",
            "Employee", "Entity", "Item", "ItemDiscount", "ItemFixedAsset", "ItemGroup", "ItemInventory", "ItemInventoryAssembly", "ItemNonInventory", "ItemOtherCharge", "ItemPayment", "ItemSalesTax", "ItemSalesTaxGroup", "ItemService", "ItemSubtotal", "Lead", "ItemSites", "JobType", "OtherName", "PaymentMethod", "PayrollItemNonWage",
            "PayrollItemWage", "PriceLevel", "SalesRep", "SalesTaxCode", "ShipMethod", "StandardTerms", "ToDo",
            "UnitOfMeasureSet", "Vehicle", "Vendor", "VendorType", "Template", "WorkersCompCode",
        }.Select(n => new Entity(n, false)),
        .. new[]
        {
            "ARRefundCreditCard", "Bill", "BillPaymentCheck", "BillPaymentCreditCard", "BuildAssembly", "Charge", "Check",
            "CreditCardCharge", "CreditCardCredit", "CreditMemo", "Deposit", "Estimate", "InventoryAdjustment",
            "Invoice", "ItemReceipt", "JournalEntry", "PurchaseOrder", "ReceivePayment", "SalesOrder", "SalesReceipt",
            "SalesTaxPaymentCheck", "TimeTracking", "Transfer", "TransferInventory", "VendorCredit",
        }.Select(n => new Entity(n, true)),
    ];

    /// <summary>Finds an entity by name, ignoring case, spaces and underscores.</summary>
    public static Entity? Find(string name) =>
        All.FirstOrDefault(e => e.Name.Equals(name.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase));
}
