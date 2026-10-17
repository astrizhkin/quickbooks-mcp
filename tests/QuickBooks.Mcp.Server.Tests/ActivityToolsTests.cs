using System.Text.Json;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class ActivityToolsTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    private ActivityWriteTools Writes() => new(new RecordWriter(_qb, new WriteExecutor(_qb, new WriteGate(WriteOp.All), new AuditLog(_log))));

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Theory]
    [InlineData("TimeTracking", "Pat", "PT2H30M")]
    [InlineData("VehicleMileage", null, "42")]
    [InlineData("ToDo", "Acme", "Call Acme")]
    public async Task Each_type_is_searched(string type, string? name, string expected)
    {
        var json = await new ActivityTools(_qb).SearchActivities(type, name, type == "ToDo" ? null : "2025-01-01");

        Assert.Equal(type + "QueryRq", _qb.SentRequest());
        Assert.Contains(expected, json);
    }

    [Fact]
    public async Task Time_entries_can_be_created_and_to_dos_updated()
    {
        var tools = Writes();

        var created = await tools.Create("TimeTracking", Json("""
            { "TxnDate": "2025-02-03", "EntityRef": { "FullName": "Pat" }, "CustomerRef": { "FullName": "Acme" }, "Duration": "PT2H30M" }
            """));
        await tools.Update("ToDo", "80000001-5000000001", Json("""{ "IsDone": true }"""));

        Assert.Contains("5E-2000000001", created);
        Assert.Contains("ToDoModRq", _qb.Requests.Select(r => r.ToString()).Last());
    }

    [Fact]
    public async Task Mileage_has_no_update_request() =>
        await ToolTestHelpers.RejectsAsync(() => Writes().Update("VehicleMileage", "5E-1000000001", Json("""{ "TotalMiles": 10 }""")));

    [Fact]
    public async Task Other_entities_are_refused() =>
        await Assert.ThrowsAsync<McpException>(() => Writes().Create("Invoice", Json("{}")));
}
