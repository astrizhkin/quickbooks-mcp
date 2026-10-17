using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class DisplayToolsTests
{
    [Theory]
    [InlineData("Customer", "ListDisplayModRq", "ListDisplayModType", "Customer", "ListID")]
    [InlineData("ItemService", "ListDisplayModRq", "ListDisplayModType", "Item", "ListID")]
    [InlineData("Account", "ListDisplayModRq", "ListDisplayModType", "Account", "ListID")]
    [InlineData("Invoice", "TxnDisplayModRq", "TxnDisplayModType", "Invoice", "TxnID")]
    [InlineData("bill", "TxnDisplayModRq", "TxnDisplayModType", "Bill", "TxnID")]
    public async Task Records_open_through_the_matching_display_request(string recordType, string request, string typeElement, string kind, string idElement)
    {
        var qb = new FixtureQbClient();

        var text = await new DisplayTools(qb).OpenInQuickBooks(recordType, "ID-1");

        Assert.Equal(request, qb.SentRequest());
        var body = qb.Requests[0].Descendants(request).Single();
        Assert.Equal(kind, (string?)body.Element(typeElement));
        Assert.Equal("ID-1", (string?)body.Element(idElement));
        Assert.Contains(kind, text);
    }

    [Theory]
    [InlineData("Class")]
    [InlineData("Template")]
    [InlineData("Bogus")]
    public async Task Types_QuickBooks_cannot_open_are_refused_without_a_request(string recordType)
    {
        var qb = new FixtureQbClient();

        await ToolTestHelpers.RejectsAsync(() => new DisplayTools(qb).OpenInQuickBooks(recordType, "1"));

        Assert.Empty(qb.Requests);
    }

    private sealed class QuickBooksNotOpen : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            buildRequest("16.0");
            return Task.FromResult("""<QBXML><QBXMLMsgsRs><ListDisplayModRs statusCode="3250" statusSeverity="Error" statusMessage="not available"/></QBXMLMsgsRs></QBXML>""");
        }
    }

    [Fact]
    public async Task A_closed_quickbooks_gives_a_clear_message()
    {
        var ex = await ToolTestHelpers.RejectsAsync(() => new DisplayTools(new QuickBooksNotOpen()).OpenInQuickBooks("Customer", "1"));

        Assert.Contains("must be open", ex.Message);
    }

    [Fact]
    public void The_openable_types_come_from_the_schema()
    {
        var schema = QuickBooks.Mcp.Server.Writes.WriteSchema.Instance;

        Assert.Contains("Customer", schema.ListDisplayTypes);
        Assert.Contains("Invoice", schema.TxnDisplayTypes);
    }
}
