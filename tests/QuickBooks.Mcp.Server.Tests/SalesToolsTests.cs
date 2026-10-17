using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class SalesToolsTests
{
    [Theory]
    [InlineData("Invoice")]
    [InlineData("salesreceipt")]
    [InlineData("CreditMemo")]
    [InlineData("Estimate")]
    [InlineData("SalesOrder")]
    [InlineData("ReceivePayment")]
    [InlineData("Deposit")]
    public async Task Each_type_queries_its_request(string type)
    {
        var qb = new FixtureQbClient();

        var json = await new SalesTools(qb).SearchSalesTransactions(type);

        Assert.Equal(type + "QueryRq", qb.SentRequest(), ignoreCase: true);
        Assert.Contains("1001", json);
    }

    [Fact]
    public async Task Filters_go_into_the_request()
    {
        var qb = new FixtureQbClient();

        await new SalesTools(qb).SearchSalesTransactions("Invoice", customer: "Acme", dateFrom: "2025-01-01", includeLineItems: true, includeLinkedTxns: true);

        Assert.Equal(["MaxReturned", "TxnDateRangeFilter", "EntityFilter", "IncludeLineItems", "IncludeLinkedTxns"], qb.SentElements());
    }

    [Fact]
    public async Task Purchase_types_are_rejected() =>
        await ToolTestHelpers.RejectsAsync(() => new SalesTools(new FixtureQbClient()).SearchSalesTransactions("Bill"));
}
