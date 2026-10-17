using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class PurchaseToolsTests
{
    [Theory]
    [InlineData("Bill")]
    [InlineData("BillPaymentCheck")]
    [InlineData("PurchaseOrder")]
    [InlineData("ItemReceipt")]
    [InlineData("VendorCredit")]
    [InlineData("check")]
    [InlineData("CreditCardCharge")]
    [InlineData("JournalEntry")]
    [InlineData("Transfer")]
    public async Task Each_type_queries_its_request(string type)
    {
        var qb = new FixtureQbClient();

        var json = await new PurchaseTools(qb).SearchPurchaseTransactions(type);

        Assert.Equal(type + "QueryRq", qb.SentRequest(), ignoreCase: true);
        Assert.Contains("2002", json);
    }

    [Fact]
    public async Task Generic_search_uses_transaction_query()
    {
        var qb = new FixtureQbClient();

        await new PurchaseTools(qb).SearchTransactions(name: "Acme", dateFrom: "2025-01-01");

        Assert.Equal("TransactionQueryRq", qb.SentRequest());
    }

    [Fact]
    public async Task Sales_types_are_rejected() =>
        await ToolTestHelpers.RejectsAsync(() => new PurchaseTools(new FixtureQbClient()).SearchPurchaseTransactions("Invoice"));
}
