using System.Collections;
using System.IO.Pipelines;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class ConfirmationAndProgressTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"qbmcp-{Guid.NewGuid()}");
    private readonly FixtureQbClient _qb = new();

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class FixedAnswer(Confirmation answer) : IUserConfirmation
    {
        public List<string> Asked { get; } = [];

        public Task<Confirmation> AskAsync(string message, CancellationToken cancellationToken)
        {
            Asked.Add(message);
            return Task.FromResult(answer);
        }
    }

    private WriteExecutor Executor(IUserConfirmation? confirmation, bool confirmAll = false) =>
        new(_qb, new WriteGate(WriteOp.All), new AuditLog(Path.Combine(_dir, "audit.jsonl")), null, null, confirmation, confirmAll);

    private static WriteCall Delete() =>
        new(WriteOp.Delete, "Customer", "ListDelRq", [new XElement("ListDelType", "Customer"), new XElement("ListID", "80000009-1000000009")]);

    private static WriteCall Add() =>
        new(WriteOp.Create, "Class", "ClassAddRq", [new XElement("ClassAdd", new XElement("Name", "East"))]);

    [Fact]
    public async Task Approval_through_the_client_replaces_the_token_step()
    {
        var asked = new FixedAnswer(Confirmation.Approved);

        var json = await Executor(asked).ExecuteAsync(Delete(), false, null, default);

        Assert.DoesNotContain("confirmation_required", json);
        Assert.Equal("ListDelRq", _qb.SentRequest());
        Assert.Contains("Delete Customer", Assert.Single(asked.Asked));
    }

    [Fact]
    public async Task A_declined_change_is_not_made_and_is_logged()
    {
        await Assert.ThrowsAsync<McpException>(() => Executor(new FixedAnswer(Confirmation.Declined)).ExecuteAsync(Delete(), false, null, default));

        Assert.Empty(_qb.Requests);
        Assert.Contains("\"outcome\":\"declined\"", File.ReadAllText(Path.Combine(_dir, "audit.jsonl")));
    }

    [Fact]
    public async Task Without_a_way_to_ask_the_token_flow_is_used()
    {
        var preview = await Executor(new FixedAnswer(Confirmation.Unavailable)).ExecuteAsync(Delete(), false, null, default);

        Assert.Contains("confirmation_token", preview);
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Creates_are_only_confirmed_when_the_server_is_set_to_ask()
    {
        var asked = new FixedAnswer(Confirmation.Approved);

        await Executor(asked).ExecuteAsync(Add(), false, null, default);
        Assert.Empty(asked.Asked);

        await Executor(asked, confirmAll: true).ExecuteAsync(Add(), false, null, default);
        Assert.Single(asked.Asked);
    }

    [Fact]
    public async Task A_dry_run_asks_nothing()
    {
        var asked = new FixedAnswer(Confirmation.Declined);

        await Executor(asked, confirmAll: true).ExecuteAsync(Delete(), true, null, default);

        Assert.Empty(asked.Asked);
    }

    private async Task<(McpClient Client, Func<Task> Stop)> StartAsync(string[] args, McpClientOptions? options, IQbClient? qb = null)
    {
        var config = ServerConfig.Load(args, new Hashtable { ["QB_AUDIT_LOG"] = Path.Combine(_dir, "audit.jsonl") });
        var toServer = new Pipe();
        var toClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(qb ?? _qb);
        services.AddQuickBooksMcp(config).WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        var provider = services.BuildServiceProvider();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _ = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);
        var client = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()), options, cancellationToken: cts.Token);
        return (client, async () =>
        {
            await client.DisposeAsync();
            await cts.CancelAsync();
            await provider.DisposeAsync();
        }
        );
    }

    private static McpClientOptions Eliciting(string action) => new()
    {
        Capabilities = new ClientCapabilities { Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() } },
        Handlers = new McpClientHandlers
        {
            ElicitationHandler = (request, _) => ValueTask.FromResult(new ElicitResult { Action = action }),
        },
    };

    [Fact]
    public async Task A_client_that_can_ask_the_user_approves_a_void_in_one_call()
    {
        var (client, stop) = await StartAsync(["--allow-writes"], Eliciting("accept"));

        var result = await client.CallToolAsync("void_transaction", new Dictionary<string, object?> { ["entity"] = "Invoice", ["id"] = "1A-1000000001" });

        Assert.True(result.IsError != true);
        Assert.Equal("TxnVoidRq", _qb.SentRequest());
        await stop();
    }

    [Fact]
    public async Task A_client_that_declines_stops_the_void()
    {
        var (client, stop) = await StartAsync(["--allow-writes"], Eliciting("decline"));

        var result = await client.CallToolAsync("void_transaction", new Dictionary<string, object?> { ["entity"] = "Invoice", ["id"] = "1A-1000000001" });

        Assert.True(result.IsError == true, string.Join("|", result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        Assert.Contains("declined", result.Content.OfType<TextContentBlock>().Single().Text);
        Assert.Empty(_qb.Requests);
        await stop();
    }

    [Fact]
    public async Task A_client_that_cannot_ask_gets_the_token_flow()
    {
        var (client, stop) = await StartAsync(["--allow-writes"], null);

        var result = await client.CallToolAsync("void_transaction", new Dictionary<string, object?> { ["entity"] = "Invoice", ["id"] = "1A-1000000001" });

        Assert.Contains("confirmation_token", result.Content.OfType<TextContentBlock>().Single().Text);
        Assert.Empty(_qb.Requests);
        await stop();
    }

    private sealed class ReportingClient : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            QbProgress.Report("Opening the QuickBooks connection");
            QbProgress.Report("Talking to QuickBooks");
            return Task.FromResult("""<QBXML><QBXMLMsgsRs><CompanyQueryRs statusCode="0"><CompanyRet><CompanyName>X</CompanyName></CompanyRet></CompanyQueryRs></QBXMLMsgsRs></QBXML>""");
        }
    }

    [Fact]
    public async Task Progress_from_the_quickbooks_layers_reaches_a_client_that_asked_for_it()
    {
        var (client, stop) = await StartAsync([], null, new ReportingClient());
        var seen = new List<string?>();

        await client.CallToolAsync("get_company", progress: new Progress<ProgressNotificationValue>(p => seen.Add(p.Message)));
        for (var i = 0; i < 50 && !seen.Contains("Talking to QuickBooks"); i++) await Task.Delay(100);

        Assert.Contains("Talking to QuickBooks", seen);
        await stop();
    }

    [Fact]
    public async Task Without_a_progress_token_nothing_is_sent_and_the_call_still_works()
    {
        var (client, stop) = await StartAsync([], null, new ReportingClient());

        var result = await client.CallToolAsync("get_company");

        Assert.Contains("CompanyName", result.Content.OfType<TextContentBlock>().Single().Text);
        await stop();
    }

    [Fact]
    public async Task A_cancelled_call_stops_waiting_for_the_company_file()
    {
        var options = new QbOptions { CompanyFile = $"x{Guid.NewGuid()}", Timeout = TimeSpan.FromSeconds(30) };
        var blocker = new LockingQbClient(new BlockingClient(), options);
        var waiter = new LockingQbClient(new FixtureQbClient(), options);
        var running = blocker.SendAsync(v => v);
        await Task.Delay(300);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.SendAsync(v => QuickBooks.Mcp.Core.QbXml.QbXmlRequest.Build(v, "HostQueryRq"), cts.Token));

        BlockingClient.Release.Set();
        await running;
    }

    private sealed class BlockingClient : IQbClient
    {
        public static readonly ManualResetEventSlim Release = new(false);

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            Release.Wait(TimeSpan.FromSeconds(10));
            return Task.FromResult("ok");
        }
    }
}
