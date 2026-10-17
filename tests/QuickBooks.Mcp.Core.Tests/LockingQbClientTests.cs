using System.Runtime.InteropServices;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class LockingQbClientTests
{
    private sealed class FakeClient(Func<int, string> reply) : IQbClient
    {
        public int Calls;
        public int Concurrent;
        public int MaxConcurrent;

        public async Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            var now = Interlocked.Increment(ref Concurrent);
            MaxConcurrent = Math.Max(MaxConcurrent, now);
            await Task.Delay(50, cancellationToken);
            Interlocked.Decrement(ref Concurrent);
            return reply(Interlocked.Increment(ref Calls));
        }
    }

    [Fact]
    public async Task Calls_to_same_file_never_overlap()
    {
        var inner = new FakeClient(_ => "ok");
        var options = new QbOptions { CompanyFile = $@"C:\test\{Guid.NewGuid()}.qbw" };
        // Two instances stand in for two server processes sharing one named mutex.
        var a = new LockingQbClient(inner, options);
        var b = new LockingQbClient(inner, options);

        await Task.WhenAll(Enumerable.Range(0, 4).Select(i => (i % 2 == 0 ? a : b).SendAsync(v => v)));

        Assert.Equal(1, inner.MaxConcurrent);
    }

    [Fact]
    public async Task Retries_com_failures_then_succeeds()
    {
        var inner = new FakeClient(n => n < 2 ? throw new COMException("busy") : "ok");
        var client = new LockingQbClient(inner, new QbOptions { CompanyFile = $"x{Guid.NewGuid()}" });

        Assert.Equal("ok", await client.SendAsync(v => v));
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Times_out()
    {
        var inner = new FakeClient(_ => "ok");
        var client = new LockingQbClient(inner, new QbOptions { CompanyFile = $"x{Guid.NewGuid()}", Timeout = TimeSpan.FromMilliseconds(10) });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SendAsync(v => v));
    }
}
