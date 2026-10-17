using System.Xml.Linq;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class WriteExecutorTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    private WriteExecutor Executor(WriteOp enabled = WriteOp.All) => new(_qb, new WriteGate(enabled), new AuditLog(_log));

    private static WriteCall Add() => new(WriteOp.Create, "Customer", "CustomerAddRq",
        [new XElement("CustomerAdd", new XElement("Name", "New Customer"), new XElement("Phone", "555-0100"))]);

    private static WriteCall Delete() => new(WriteOp.Delete, "Customer", "ListDelRq",
        [new XElement("ListDelType", "Customer"), new XElement("ListID", "80000009-1000000009")]);

    [Fact]
    public async Task Dry_run_shows_the_request_and_sends_nothing()
    {
        var json = await Executor().ExecuteAsync(Add(), dryRun: true, null, default);

        Assert.Contains("CustomerAddRq", json);
        Assert.Empty(_qb.Requests);
        Assert.False(File.Exists(_log));
    }

    [Fact]
    public async Task Create_sends_and_audits_ids_without_data()
    {
        var json = await Executor().ExecuteAsync(Add(), dryRun: false, null, default);

        Assert.Contains("80000009-1000000009", json);
        var audit = File.ReadAllText(_log);
        Assert.Contains("\"outcome\":\"ok\"", audit);
        Assert.Contains("80000009-1000000009", audit);
        Assert.DoesNotContain("555-0100", audit);
        Assert.DoesNotContain("New Customer", audit);
    }

    [Fact]
    public async Task Delete_needs_a_token_from_a_preview()
    {
        var executor = Executor();

        var preview = await executor.ExecuteAsync(Delete(), false, null, default);

        Assert.Contains("confirmation_required", preview);
        Assert.Empty(_qb.Requests);
        var token = System.Text.Json.JsonDocument.Parse(preview).RootElement.GetProperty("confirmation_token").GetString();

        await executor.ExecuteAsync(Delete(), false, token, default);

        Assert.Single(_qb.Requests);
        await Assert.ThrowsAsync<McpException>(() => executor.ExecuteAsync(Delete(), false, token, default)); // single use
    }

    [Fact]
    public async Task Token_is_bound_to_its_request()
    {
        var executor = Executor();
        var preview = await executor.ExecuteAsync(Delete(), false, null, default);
        var token = System.Text.Json.JsonDocument.Parse(preview).RootElement.GetProperty("confirmation_token").GetString();
        var other = new WriteCall(WriteOp.Delete, "Customer", "ListDelRq", [new XElement("ListDelType", "Customer"), new XElement("ListID", "OTHER")]);

        await Assert.ThrowsAsync<McpException>(() => executor.ExecuteAsync(other, false, token, default));
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Disabled_operation_is_refused_before_anything_happens()
    {
        await Assert.ThrowsAsync<McpException>(() => Executor(WriteOp.None).ExecuteAsync(Add(), false, null, default));

        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public async Task Failures_are_audited()
    {
        var call = new WriteCall(WriteOp.Create, "Class", "ClassAddRq", [new XElement("ClassAdd", new XElement("Name", "East"))]);

        await Assert.ThrowsAsync<FileNotFoundException>(() => Executor().ExecuteAsync(call, false, null, default));

        Assert.Contains("\"outcome\":\"failed\"", File.ReadAllText(_log));
    }
}
