using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class CachingQbClientTests
{
    private sealed class CountingClient : IQbClient
    {
        public int Calls;

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            buildRequest("16.0");
            return Task.FromResult($"reply-{++Calls}");
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static Func<string, string> Request(string name, params string[] fullNames) =>
        v => QbXmlRequest.Build(v, name, fullNames.Select(n => new System.Xml.Linq.XElement("FullName", n)));

    [Fact]
    public async Task Stable_lists_are_remembered_for_a_short_time()
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30), new Clock());

        Assert.Equal("reply-1", await cache.SendAsync(Request("AccountQueryRq")));
        Assert.Equal("reply-1", await cache.SendAsync(Request("AccountQueryRq")));
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Different_requests_are_cached_separately()
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30));

        await cache.SendAsync(Request("ItemQueryRq", "Widget"));
        await cache.SendAsync(Request("ItemQueryRq", "Gadget"));

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Entries_expire()
    {
        var clock = new Clock();
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30), clock);

        await cache.SendAsync(Request("ClassQueryRq"));
        clock.Now += TimeSpan.FromSeconds(31);
        var again = await cache.SendAsync(Request("ClassQueryRq"));

        Assert.Equal("reply-2", again);
    }

    [Theory]
    [InlineData("CustomerQueryRq")]
    [InlineData("InvoiceQueryRq")]
    [InlineData("GeneralSummaryReportQueryRq")]
    public async Task Things_that_change_often_are_never_cached(string request)
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30));

        await cache.SendAsync(Request(request));
        await cache.SendAsync(Request(request));

        Assert.Equal(2, inner.Calls);
    }

    [Theory]
    [InlineData("CustomerAddRq")]
    [InlineData("ItemServiceModRq")]
    [InlineData("ListDelRq")]
    [InlineData("TxnVoidRq")]
    public async Task Any_write_empties_the_cache(string write)
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30));
        await cache.SendAsync(Request("AccountQueryRq"));

        await cache.SendAsync(Request(write));
        await cache.SendAsync(Request("AccountQueryRq"));

        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task A_failed_write_also_empties_the_cache()
    {
        var inner = new FailingOnWrite();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30));
        await cache.SendAsync(Request("AccountQueryRq"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.SendAsync(Request("CustomerAddRq")));
        await cache.SendAsync(Request("AccountQueryRq"));

        Assert.Equal(2, inner.Reads);
    }

    private sealed class FailingOnWrite : IQbClient
    {
        public int Reads;

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default) =>
            buildRequest("16.0").Contains("AddRq") ? throw new InvalidOperationException("boom") : Task.FromResult($"reply-{++Reads}");
    }

    [Fact]
    public async Task A_lifetime_of_zero_turns_the_cache_off()
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.Zero);

        await cache.SendAsync(Request("AccountQueryRq"));
        await cache.SendAsync(Request("AccountQueryRq"));

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Clear_forgets_everything()
    {
        var inner = new CountingClient();
        var cache = new CachingQbClient(inner, TimeSpan.FromSeconds(30));
        await cache.SendAsync(Request("AccountQueryRq"));

        cache.Clear();
        await cache.SendAsync(Request("AccountQueryRq"));

        Assert.Equal(2, inner.Calls);
    }
}
