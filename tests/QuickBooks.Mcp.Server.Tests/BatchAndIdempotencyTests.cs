using System.Text.Json;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class BatchAndIdempotencyTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"qbmcp-{Guid.NewGuid()}");
    private readonly FixtureQbClient _qb = new();

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private RecordWriter Writer() => new(_qb, new WriteExecutor(_qb, new WriteGate(WriteOp.All),
        new AuditLog(Path.Combine(_dir, "audit.jsonl")), new IdempotencyStore(Path.Combine(_dir, "idempotency.jsonl"))));

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task Same_key_and_request_creates_only_once()
    {
        var tools = new ListWriteTools(Writer());
        var data = Json("""{ "Name": "New Customer" }""");

        var first = await tools.Create("Customer", data, idempotencyKey: "key-1");
        var second = await tools.Create("Customer", data, idempotencyKey: "key-1");

        Assert.Single(_qb.Requests);
        Assert.DoesNotContain("duplicate", first);
        Assert.Contains("\"duplicate\":true", second);
        Assert.Contains("80000009-1000000009", second);
        Assert.Contains("\"outcome\":\"duplicate\"", File.ReadAllText(Path.Combine(_dir, "audit.jsonl")));
    }

    [Fact]
    public async Task Same_key_for_a_different_request_is_refused()
    {
        var tools = new ListWriteTools(Writer());
        await tools.Create("Customer", Json("""{ "Name": "A" }"""), idempotencyKey: "key-1");

        await Assert.ThrowsAsync<McpException>(() => tools.Create("Customer", Json("""{ "Name": "B" }"""), idempotencyKey: "key-1"));
        Assert.Single(_qb.Requests);
    }

    [Fact]
    public async Task Without_a_key_every_call_creates()
    {
        var tools = new ListWriteTools(Writer());
        var data = Json("""{ "Name": "A" }""");

        await tools.Create("Customer", data);
        await tools.Create("Customer", data);

        Assert.Equal(2, _qb.Requests.Count);
    }

    [Fact]
    public async Task Dry_runs_do_not_use_up_a_key()
    {
        var tools = new ListWriteTools(Writer());
        var data = Json("""{ "Name": "A" }""");

        await tools.Create("Customer", data, dryRun: true, idempotencyKey: "key-1");
        await tools.Create("Customer", data, idempotencyKey: "key-1");

        Assert.Single(_qb.Requests);
    }

    [Fact]
    public async Task Batch_reports_each_item_and_continues_after_a_failure()
    {
        var items = Json("""
            [ { "entity": "Customer", "data": { "Name": "A" } },
              { "entity": "Customer", "data": { "Bogus": 1 } },
              { "entity": "Nothing", "data": {} },
              { "entity": "Customer", "data": { "Name": "B" } } ]
            """);

        var json = JsonDocument.Parse(await new BatchWriteTools(Writer()).CreateRecords(items)).RootElement;

        Assert.Equal(2, json.GetProperty("succeeded").GetInt32());
        Assert.Equal(2, json.GetProperty("failed").GetInt32());
        Assert.Equal([true, false, false, true], json.GetProperty("results").EnumerateArray().Select(r => r.GetProperty("ok").GetBoolean()));
        Assert.Contains("Bogus", json.GetProperty("results")[1].GetProperty("error").GetString());
    }

    [Fact]
    public async Task Batch_can_stop_at_the_first_failure()
    {
        var items = Json("""[ { "entity": "Customer", "data": { "Bogus": 1 } }, { "entity": "Customer", "data": { "Name": "B" } } ]""");

        var json = JsonDocument.Parse(await new BatchWriteTools(Writer()).CreateRecords(items, stopOnError: true)).RootElement;

        Assert.Single(json.GetProperty("results").EnumerateArray());
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Batch_retry_with_keys_does_not_duplicate()
    {
        var items = Json("""[ { "entity": "Customer", "data": { "Name": "A" }, "idempotencyKey": "k1" }, { "entity": "Customer", "data": { "Name": "B" }, "idempotencyKey": "k2" } ]""");
        var tools = new BatchWriteTools(Writer());

        await tools.CreateRecords(items);
        var retry = await tools.CreateRecords(items);

        Assert.Equal(2, _qb.Requests.Count);
        Assert.Contains("duplicate", retry);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task Batch_needs_a_non_empty_array(string json) =>
        await Assert.ThrowsAsync<McpException>(() => new BatchWriteTools(Writer()).CreateRecords(Json(json)));
}
