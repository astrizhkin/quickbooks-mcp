using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class RecordWriterTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    private RecordWriter Writer(WriteOp enabled = WriteOp.All) =>
        new(_qb, new WriteExecutor(_qb, new WriteGate(enabled), new AuditLog(_log)));

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void Fields_are_ordered_by_the_schema_whatever_order_they_arrive_in()
    {
        var data = (JsonObject)JsonNode.Parse("""{ "Phone": "555-0100", "BillAddress": { "City": "Reno", "Addr1": "1 Main St" }, "Name": "Acme" }""")!;

        var names = WriteSchema.Instance.Build("Customer", "Add", data).Elements().Select(e => e.Name.LocalName);
        var address = WriteSchema.Instance.Build("Customer", "Add", data).Element("BillAddress")!.Elements().Select(e => e.Name.LocalName);

        Assert.Equal(["Name", "BillAddress", "Phone"], names);
        Assert.Equal(["Addr1", "City"], address);
    }

    [Fact]
    public void Unknown_fields_are_refused_with_the_valid_ones()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WriteSchema.Instance.Build("Customer", "Add", (JsonObject)JsonNode.Parse("""{ "Nmae": "x" }""")!));

        Assert.Contains("'Nmae'", ex.Message);
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void Repeated_elements_take_arrays_and_values_are_typed()
    {
        var data = (JsonObject)JsonNode.Parse("""
            { "CustomerRef": { "FullName": "Acme" }, "IsPending": false,
              "InvoiceLineAdd": [ { "ItemRef": { "FullName": "Widget" }, "Quantity": 2, "Rate": 9.5 }, { "Desc": "Setup", "Amount": 10 } ] }
            """)!;

        var invoice = WriteSchema.Instance.Build("Invoice", "Add", data);

        Assert.Equal("false", (string?)invoice.Element("IsPending"));
        Assert.Equal(2, invoice.Elements("InvoiceLineAdd").Count());
        Assert.Equal("9.5", (string?)invoice.Elements("InvoiceLineAdd").First().Element("Rate"));
    }

    [Fact]
    public async Task Create_sends_an_add_request_and_returns_the_new_id()
    {
        var json = await Writer().CreateAsync(["Customer"], "customer", Json("""{ "Name": "New Customer", "Phone": "555-0100" }"""), false, default);

        Assert.Equal("CustomerAddRq", _qb.SentRequest());
        Assert.Contains("80000009-1000000009", json);
    }

    [Fact]
    public async Task Create_dry_run_sends_nothing()
    {
        var json = await Writer().CreateAsync(["Customer"], "Customer", Json("""{ "Name": "New Customer" }"""), true, default);

        Assert.Contains("dry_run", json);
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Update_reads_the_edit_sequence_then_modifies()
    {
        await Writer().UpdateAsync(["Customer"], "Customer", "80000001-1000000001", Json("""{ "Phone": "555-0199" }"""), false, default);

        Assert.Equal(["CustomerQueryRq", "CustomerModRq"], _qb.Requests.Select(r => r.Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName));
        var mod = _qb.Requests[1].Root!.Element("QBXMLMsgsRq")!.Element("CustomerModRq")!.Element("CustomerMod")!;
        Assert.Equal(["ListID", "EditSequence", "Phone"], mod.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("1234", (string?)mod.Element("EditSequence"));
    }

    [Fact]
    public async Task Transactions_are_updated_by_txn_id()
    {
        await Writer().UpdateAsync(["Invoice"], "Invoice", "1A-1000000001", Json("""{ "Memo": "Thanks" }"""), false, default);

        var mod = _qb.Requests[1].Root!.Element("QBXMLMsgsRq")!.Element("InvoiceModRq")!.Element("InvoiceMod")!;
        Assert.Equal(["TxnID", "EditSequence", "Memo"], mod.Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public async Task Entities_outside_the_tools_set_are_refused() =>
        await Assert.ThrowsAsync<McpException>(() => Writer().CreateAsync(["Customer"], "Invoice", Json("{}"), false, default));

    [Fact]
    public async Task Data_must_be_an_object() =>
        await Assert.ThrowsAsync<McpException>(() => Writer().CreateAsync(["Customer"], "Customer", Json("[1]"), false, default));

    [Fact]
    public async Task Tools_report_bad_fields_as_tool_errors()
    {
        var tools = new ListWriteTools(Writer());

        await Assert.ThrowsAsync<McpException>(() => tools.Create("Customer", Json("""{ "Bogus": 1 }"""), true));
    }

    [Fact]
    public void Every_tool_entity_can_be_created()
    {
        string[] entities =
        [
            "Account", "Class", "Customer", "Vendor", "Employee", "OtherName", "Lead", "ItemService", "ItemNonInventory", "ItemInventory",
            "ItemInventoryAssembly", "ItemOtherCharge", "ItemDiscount", "ItemSalesTax", "ItemSalesTaxGroup", "ItemGroup", "ItemSubtotal",
            "ItemPayment", "ItemFixedAsset", "StandardTerms", "DateDrivenTerms", "PaymentMethod", "SalesTaxCode", "SalesRep", "Vehicle",
            "BillingRate", "PriceLevel", "WorkersCompCode", "CustomerType", "VendorType", "JobType", "ShipMethod", "CustomerMsg",
            "Invoice", "SalesReceipt", "CreditMemo", "Estimate", "SalesOrder", "ReceivePayment", "ARRefundCreditCard", "Charge", "Deposit",
            "Bill", "BillPaymentCheck", "BillPaymentCreditCard", "PurchaseOrder", "ItemReceipt", "VendorCredit", "Check", "CreditCardCharge",
            "CreditCardCredit", "SalesTaxPaymentCheck", "Transfer", "JournalEntry",
        ];

        Assert.All(entities, e => Assert.True(WriteSchema.Instance.Has(e, "Add"), $"{e} has no Add request"));
    }
}

public sealed class JournalEntryTests
{
    private readonly FixtureQbClient _qb = new();

    private RecordWriter Writer() => new(_qb, new WriteExecutor(_qb, new WriteGate(WriteOp.All), new AuditLog(Path.Combine(Path.GetTempPath(), $"a-{Guid.NewGuid()}.jsonl"))));

    private static JsonElement Entry(string credit) => JsonDocument.Parse($$"""
        { "TxnDate": "2025-01-31",
          "JournalDebitLine": [ { "AccountRef": { "FullName": "Checking" }, "Amount": 100.00 } ],
          "JournalCreditLine": [ { "AccountRef": { "FullName": "Income" }, "Amount": {{credit}} } ] }
        """).RootElement.Clone();

    [Fact]
    public async Task Unbalanced_entries_are_refused_before_sending()
    {
        await Assert.ThrowsAsync<McpException>(() => Writer().CreateAsync(["JournalEntry"], "JournalEntry", Entry("90.00"), false, default));

        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Balanced_entries_pass_the_balance_check()
    {
        // No JournalEntryAdd fixture exists, so reaching the fixture lookup proves the check passed.
        await Assert.ThrowsAsync<FileNotFoundException>(() => Writer().CreateAsync(["JournalEntry"], "JournalEntry", Entry("100.00"), false, default));
    }
}

public class CardDataTests
{
    [Theory]
    [InlineData("ReceivePayment", "Add", """{ "CreditCardTxnInfo": { "CreditCardTxnInputInfo": { "CreditCardNumber": "4111111111111111" } } }""")]
    [InlineData("SalesReceipt", "Add", """{ "CreditCardTxnInfo": {} }""")]
    [InlineData("Customer", "Add", """{ "CreditCardInfo": { "CreditCardNumber": "4111111111111111" } }""")]
    public void Card_data_is_refused_on_writes(string entity, string suffix, string json)
    {
        var ex = Assert.Throws<ArgumentException>(() => WriteSchema.Instance.Build(entity, suffix, (JsonObject)JsonNode.Parse(json)!));

        Assert.Contains("card data is not accepted", ex.Message);
    }

    [Fact]
    public void Card_fields_are_hidden_from_the_field_listing()
    {
        var tree = WriteSchema.Instance.Describe("ReceivePayment", "Add").ToJsonString();

        Assert.DoesNotContain("CreditCardTxn", tree);
        Assert.Contains("CustomerRef", tree);
    }

    [Fact]
    public void Credit_card_charges_to_an_account_are_still_allowed()
    {
        var data = (JsonObject)JsonNode.Parse("""{ "AccountRef": { "FullName": "Visa" }, "TxnDate": "2025-01-01" }""")!;

        var element = WriteSchema.Instance.Build("CreditCardCharge", "Add", data);

        Assert.NotNull(element.Element("TxnDate"));
    }
}
