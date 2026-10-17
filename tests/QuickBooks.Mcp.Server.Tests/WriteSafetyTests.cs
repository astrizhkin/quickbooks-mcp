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

/// <summary>The properties that make the write path safe, each proven end to end.</summary>
public sealed class WriteSafetyTests : IDisposable
{
    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"qbmcp-{Guid.NewGuid()}");
    private readonly FixtureQbClient _qb = new();
    private readonly FakeClock _clock = new();

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private string AuditPath => Path.Combine(_dir, "audit.jsonl");

    private WriteExecutor Executor(WriteOp enabled) =>
        new(_qb, new WriteGate(enabled), new AuditLog(AuditPath), new IdempotencyStore(Path.Combine(_dir, "i.jsonl")), _clock);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private IEnumerable<string> Sent() =>
        _qb.Requests.Select(r => r.Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName);

    private static WriteCall DeleteCustomer(string id) =>
        new(WriteOp.Delete, "Customer", "ListDelRq", [new XElement("ListDelType", "Customer"), new XElement("ListID", id)]);

    private static string TokenOf(string preview) =>
        JsonDocument.Parse(preview).RootElement.GetProperty("confirmation_token").GetString()!;

    [Fact]
    public async Task With_defaults_a_write_tool_cannot_even_be_called_over_the_protocol()
    {
        var config = ServerConfig.Load([], new Hashtable());
        var toServer = new Pipe();
        var toClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IQbClient>(_qb);
        services.AddQuickBooksMcp(config).WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        await using var provider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _ = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);
        await using var client = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()), cancellationToken: cts.Token);

        foreach (var tool in new[] { "create_list_item", "update_list_item", "delete_record", "void_transaction", "create_records" })
        {
            var arguments = new Dictionary<string, object?> { ["entity"] = "Customer", ["id"] = "1" };
            var outcome = await Record.ExceptionAsync(async () =>
            {
                var result = await client.CallToolAsync(tool, arguments, cancellationToken: cts.Token);
                Assert.True(result.IsError == true, $"{tool} was callable");
            });
            if (outcome is not null) Assert.IsAssignableFrom<McpException>(outcome);
        }

        Assert.Empty(_qb.Requests);
        await cts.CancelAsync();
    }

    [Fact]
    public async Task Every_write_tool_refuses_when_its_operation_is_off_even_if_called_directly()
    {
        var off = Executor(WriteOp.None);
        var writer = new RecordWriter(_qb, off);

        await Assert.ThrowsAsync<McpException>(() => new ListWriteTools(writer).Create("Customer", Json("""{ "Name": "A" }""")));
        await Assert.ThrowsAsync<McpException>(() => new SalesWriteTools(writer).Update("Invoice", "1A-1", Json("""{ "Memo": "x" }""")));
        await Assert.ThrowsAsync<McpException>(() => new PurchaseWriteTools(writer).Create("Bill", Json("{}")));
        await Assert.ThrowsAsync<McpException>(() => new DeleteTools(off).Delete("Customer", "1"));
        await Assert.ThrowsAsync<McpException>(() => new DeleteTools(off).Void("Invoice", "1"));
        var batch = JsonDocument.Parse(await new BatchWriteTools(writer).CreateRecords(
            Json("""[ { "entity": "Customer", "data": { "Name": "A" } } ]"""))).RootElement;

        Assert.Equal(0, batch.GetProperty("succeeded").GetInt32());
        Assert.DoesNotContain(Sent(), s => s.EndsWith("AddRq") || s.EndsWith("ModRq") || s.EndsWith("DelRq") || s.EndsWith("VoidRq"));
    }

    [Fact]
    public async Task Dry_run_sends_no_write_for_create_and_update()
    {
        var writer = new RecordWriter(_qb, Executor(WriteOp.All));

        await writer.CreateAsync(["Customer"], "Customer", Json("""{ "Name": "A" }"""), true, default);
        await writer.UpdateAsync(["Customer"], "Customer", "80000001-1000000001", Json("""{ "Phone": "1" }"""), true, default);

        Assert.DoesNotContain(Sent(), s => s.EndsWith("AddRq") || s.EndsWith("ModRq"));
        Assert.False(File.Exists(AuditPath));
    }

    [Fact]
    public async Task Confirmation_tokens_expire()
    {
        var executor = Executor(WriteOp.All);
        var call = DeleteCustomer("80000009-1000000009");
        var token = TokenOf(await executor.ExecuteAsync(call, false, null, default));

        _clock.Now += TimeSpan.FromMinutes(6);

        await Assert.ThrowsAsync<McpException>(() => executor.ExecuteAsync(call, false, token, default));
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Tokens_still_work_before_they_expire()
    {
        var executor = Executor(WriteOp.All);
        var call = DeleteCustomer("80000009-1000000009");
        var token = TokenOf(await executor.ExecuteAsync(call, false, null, default));

        _clock.Now += TimeSpan.FromMinutes(4);
        await executor.ExecuteAsync(call, false, token, default);

        Assert.Single(_qb.Requests);
    }

    [Fact]
    public async Task Every_kind_of_write_leaves_an_audit_entry()
    {
        var writer = new RecordWriter(_qb, Executor(WriteOp.All));
        var delete = new DeleteTools(Executor(WriteOp.All));

        await writer.CreateAsync(["Customer"], "Customer", Json("""{ "Name": "A" }"""), false, default);
        await writer.UpdateAsync(["Customer"], "Customer", "80000001-1000000001", Json("""{ "Phone": "1" }"""), false, default);
        var preview = await delete.Delete("Customer", "80000009-1000000009");
        await delete.Delete("Customer", "80000009-1000000009", TokenOf(preview));

        var log = File.ReadAllLines(AuditPath);
        Assert.Equal(["Create:ok", "Update:ok", "Delete:previewed", "Delete:ok"],
            log.Select(l => JsonDocument.Parse(l).RootElement).Select(e => $"{e.GetProperty("op").GetString()}:{e.GetProperty("outcome").GetString()}"));
        Assert.All(log, l => Assert.DoesNotContain("Phone", l));
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("""{ "Bogus": 1 }""")]
    [InlineData("""{ "Name": { "nested": "object" } }""")]
    [InlineData("""{ "Name": ["a", "b"] }""")]
    [InlineData("""{ "BillAddress": { "Nope": "x" } }""")]
    [InlineData("""{ "BillAddress": "not an object" }""")]
    public async Task Malformed_input_is_rejected_before_it_reaches_quickbooks(string data)
    {
        var tools = new ListWriteTools(new RecordWriter(_qb, Executor(WriteOp.All)));

        await Assert.ThrowsAsync<McpException>(() => tools.Create("Customer", Json(data)));

        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Ids_are_escaped_not_injected_into_the_request()
    {
        const string evil = "</ListID><Evil/><ListID>";

        var preview = await Executor(WriteOp.All).ExecuteAsync(DeleteCustomer(evil), false, null, default);
        var request = JsonDocument.Parse(preview).RootElement.GetProperty("request").GetString()!;

        Assert.DoesNotContain("<Evil/>", request);
        Assert.Equal(evil, XDocument.Parse(request).Descendants("ListID").Single().Value);
    }

    [Fact]
    public async Task Unknown_entities_and_wrong_operations_are_rejected()
    {
        var tools = new ListWriteTools(new RecordWriter(_qb, Executor(WriteOp.All)));

        await Assert.ThrowsAsync<McpException>(() => tools.Create("Invoice", Json("{}")));
        await Assert.ThrowsAsync<McpException>(() => tools.Create("", Json("{}")));
        await Assert.ThrowsAsync<McpException>(() => new DeleteTools(Executor(WriteOp.All)).Void("Customer", "1"));
        Assert.Empty(_qb.Requests);
    }
}
