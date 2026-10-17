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
        var options = new QbOptions { LockName = Guid.NewGuid().ToString() };
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
        var client = new LockingQbClient(inner, new QbOptions { LockName = Guid.NewGuid().ToString() });

        Assert.Equal("ok", await client.SendAsync(v => v));
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Times_out()
    {
        var inner = new FakeClient(_ => "ok");
        var client = new LockingQbClient(inner, new QbOptions { LockName = Guid.NewGuid().ToString(), Timeout = TimeSpan.FromMilliseconds(10) });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SendAsync(v => v));
    }
}

public class MachineWideLockTests
{
    [Fact]
    public async Task Servers_configured_for_different_files_still_take_turns()
    {
        var inner = new OverlapDetector();
        var name = Guid.NewGuid().ToString();
        var a = new LockingQbClient(inner, new QbOptions { CompanyFile = "C:/Books/A.qbw", LockName = name });
        var b = new LockingQbClient(inner, new QbOptions { CompanyFile = "C:/Books/B.qbw", LockName = name });

        await Task.WhenAll(a.SendAsync(v => v), b.SendAsync(v => v), a.SendAsync(v => v), b.SendAsync(v => v));

        Assert.Equal(1, inner.MaxConcurrent);
    }

    private sealed class OverlapDetector : IQbClient
    {
        private int _now;
        public int MaxConcurrent;

        public async Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            MaxConcurrent = Math.Max(MaxConcurrent, Interlocked.Increment(ref _now));
            await Task.Delay(40, cancellationToken);
            Interlocked.Decrement(ref _now);
            return "ok";
        }
    }
}
