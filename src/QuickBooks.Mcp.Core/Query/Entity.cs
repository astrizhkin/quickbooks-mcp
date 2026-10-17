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
            "Employee", "Entity", "InventorySite", "Item", "ItemDiscount", "ItemFixedAsset", "ItemGroup", "ItemInventory", "ItemInventoryAssembly", "ItemNonInventory", "ItemOtherCharge", "ItemPayment", "ItemSalesTax", "ItemSalesTaxGroup", "ItemService", "ItemSubtotal", "Lead", "JobType", "OtherName", "PaymentMethod", "PayrollItemNonWage",
            "PayrollItemWage", "PriceLevel", "SalesRep", "SalesTaxCode", "ShipMethod", "StandardTerms", "ToDo",
            "UnitOfMeasureSet", "Vehicle", "Vendor", "VendorType", "Template", "WorkersCompCode",
        }.Select(n => new Entity(n, false)),
        .. new[]
        {
            "ARRefundCreditCard", "Bill", "BillPaymentCheck", "BillPaymentCreditCard", "BuildAssembly", "Charge", "Check",
            "CreditCardCharge", "CreditCardCredit", "CreditMemo", "Deposit", "Estimate", "InventoryAdjustment",
            "Invoice", "ItemReceipt", "JournalEntry", "PurchaseOrder", "ReceivePayment", "SalesOrder", "SalesReceipt",
            "SalesTaxPaymentCheck", "TimeTracking", "Transaction", "Transfer", "TransferInventory", "VehicleMileage", "VendorCredit",
        }.Select(n => new Entity(n, true)),
    ];

    /// <summary>Finds an entity by name, ignoring case, spaces and underscores.</summary>
    public static Entity? Find(string name) =>
        All.FirstOrDefault(e => e.Name.Equals(name.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase));

    // What the qbXML schema allows differs per request; these are the exceptions to the common shape.
    private static readonly HashSet<string> LinkedTxns =
        ["Invoice", "Estimate", "SalesOrder", "CreditMemo", "Charge", "PurchaseOrder", "Bill", "ItemReceipt", "VendorCredit", "Check"];

    private static readonly HashSet<string> NoLineItems = ["Charge", "TimeTracking", "Transfer", "BuildAssembly", "Transaction", "VehicleMileage"];
    private static readonly HashSet<string> NoActiveStatus = ["BillingRate", "Lead"];
    private static readonly HashSet<string> NoEntityFilter = ["BuildAssembly", "Transfer", "VehicleMileage"];
    private static readonly HashSet<string> NoAccountFilter = ["SalesOrder", "BuildAssembly", "Transfer", "TimeTracking", "VehicleMileage"];

    /// <summary>Whether the request accepts IncludeLinkedTxns.</summary>
    public bool HasLinkedTxns => LinkedTxns.Contains(Name);

    /// <summary>Whether the request accepts IncludeLineItems.</summary>
    public bool HasLineItems => IsTransaction && !NoLineItems.Contains(Name);

    /// <summary>Whether the request accepts ActiveStatus.</summary>
    public bool HasActiveStatus => !IsTransaction && !NoActiveStatus.Contains(Name);

    /// <summary>Whether the request accepts an EntityFilter.</summary>
    public bool HasEntityFilter => IsTransaction && !NoEntityFilter.Contains(Name);

    /// <summary>Whether the request accepts an AccountFilter.</summary>
    public bool HasAccountFilter => IsTransaction && !NoAccountFilter.Contains(Name);

    /// <summary>Templates can only be listed: the request has no filters or lookups.</summary>
    public bool Unfilterable => Name == "Template";

    /// <summary>The element used to look a list item up by name, or null if it cannot be.</summary>
    public string? NameLookup => IsTransaction || Unfilterable || Name == "ToDo" ? null : Name == "Lead" ? "Name" : "FullName";

    /// <summary>Whether the request accepts MaxReturned (inventory sites do not; the page is cut client-side).</summary>
    public bool HasMaxReturned => Name != "InventorySite";
}
