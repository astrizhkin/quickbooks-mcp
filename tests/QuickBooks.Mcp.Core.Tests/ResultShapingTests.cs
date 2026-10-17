using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class ResultShapingTests
{
    private sealed class CannedClient(string reply) : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default) =>
            Task.FromResult(reply);
    }

    [Fact]
    public async Task Sensitive_values_are_masked_and_others_kept()
    {
        var qb = new RedactingQbClient(new CannedClient("""
            <?xml version="1.0"?><QBXML><QBXMLMsgsRs><EmployeeQueryRs statusCode="0"><EmployeeRet>
              <Name>Pat</Name><SSN>123-45-6789</SSN><Balance>10.50</Balance>
              <EmployeePayrollInfo><PayPeriod>Weekly</PayPeriod></EmployeePayrollInfo>
            </EmployeeRet></EmployeeQueryRs></QBXMLMsgsRs></QBXML>
            """));

        var reply = await qb.SendAsync(v => v);

        Assert.DoesNotContain("123-45-6789", reply);
        Assert.DoesNotContain("Weekly", reply);
        Assert.Contains("<Name>Pat</Name>", reply);
        Assert.Contains("<Balance>10.50</Balance>", reply);
    }

    [Fact]
    public async Task Oversized_pages_are_cut_with_a_cursor()
    {
        var big = new string('x', 60_000);
        var records = string.Concat(Enumerable.Range(0, 3).Select(i => $"<CustomerRet><Name>{big}</Name></CustomerRet>"));
        var qb = new CannedClient($"""<QBXML><QBXMLMsgsRs><CustomerQueryRs statusCode="0">{records}</CustomerQueryRs></QBXMLMsgsRs></QBXML>""");

        var result = await new QueryRunner(qb).RunAsync(Entity.Find("Customer")!, new QueryFilters(), 10, null);

        Assert.Equal(1, (int)result["count"]!);
        Assert.Equal("1", (string?)result["next_cursor"]);
    }
}

public class CardRedactionTests
{
    private sealed class CannedClient(string reply) : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default) => Task.FromResult(reply);
    }

    [Fact]
    public async Task Card_processing_blocks_are_masked()
    {
        var qb = new RedactingQbClient(new CannedClient("""
            <QBXML><QBXMLMsgsRs><ReceivePaymentQueryRs statusCode="0"><ReceivePaymentRet><TxnID>1</TxnID>
              <CreditCardTxnInfo><CreditCardTxnResultInfo><AuthorizationCode>ABC123</AuthorizationCode></CreditCardTxnResultInfo></CreditCardTxnInfo>
            </ReceivePaymentRet></ReceivePaymentQueryRs></QBXMLMsgsRs></QBXML>
            """));

        var reply = await qb.SendAsync(v => v);

        Assert.DoesNotContain("ABC123", reply);
        Assert.Contains("<TxnID>1</TxnID>", reply);
    }
}
