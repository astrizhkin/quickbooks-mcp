using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class QbClientExtensionsTests
{
    private sealed class FakeClient(string reply) : IQbClient
    {
        public string? Sent { get; private set; }

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            Sent = buildRequest("16.0");
            return Task.FromResult(reply);
        }
    }

    [Fact]
    public async Task SendAsync_builds_request_and_parses_reply()
    {
        var fake = new FakeClient("""<QBXML><QBXMLMsgsRs><HostQueryRs statusCode="0"><HostRet/></HostQueryRs></QBXMLMsgsRs></QBXML>""");

        var response = await fake.SendAsync("HostQueryRq");

        Assert.Contains("<HostQueryRq", fake.Sent);
        Assert.Single(response.Records);
    }

    [Fact]
    public async Task SendAsync_surfaces_qb_errors()
    {
        var fake = new FakeClient("""<QBXML><QBXMLMsgsRs><HostQueryRs statusCode="3250" statusSeverity="Error" statusMessage="nope"/></QBXMLMsgsRs></QBXML>""");

        await Assert.ThrowsAsync<QbException>(() => fake.SendAsync("HostQueryRq"));
    }
}
