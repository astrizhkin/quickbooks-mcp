using System.Text.Json;
using ModelContextProtocol;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class DeleteToolsTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");
    private readonly FixtureQbClient _qb = new();

    public void Dispose() => File.Delete(_log);

    private DeleteTools Tools(WriteOp enabled = WriteOp.All, string[]? allow = null) =>
        new(new WriteExecutor(_qb, new WriteGate(enabled, allow), new AuditLog(_log)));

    private static string Token(string preview) => JsonDocument.Parse(preview).RootElement.GetProperty("confirmation_token").GetString()!;

    [Theory]
    [InlineData("Customer", "ListDelRq")]
    [InlineData("Invoice", "TxnDelRq")]
    public async Task Delete_previews_first_then_deletes_with_the_token(string entity, string request)
    {
        var tools = Tools();

        var preview = await tools.Delete(entity, "ID-1");
        Assert.Empty(_qb.Requests);
        Assert.Contains(request, preview);

        await tools.Delete(entity, "ID-1", Token(preview));

        Assert.Equal(request, _qb.SentRequest());
    }

    [Fact]
    public async Task Void_previews_first_then_voids_with_the_token()
    {
        var tools = Tools();

        var preview = await tools.Void("invoice", "1A-1000000001");
        await tools.Void("Invoice", "1A-1000000001", Token(preview));

        Assert.Equal("TxnVoidRq", _qb.SentRequest());
        Assert.Contains("\"outcome\":\"ok\"", File.ReadAllText(_log));
    }

    [Fact]
    public async Task Token_for_one_record_cannot_delete_another()
    {
        var tools = Tools();
        var preview = await tools.Delete("Customer", "ID-1");

        await Assert.ThrowsAsync<McpException>(() => tools.Delete("Customer", "ID-2", Token(preview)));
        Assert.Empty(_qb.Requests);
    }

    [Theory]
    [InlineData("Estimate")] // can be deleted but not voided
    [InlineData("Bogus")]
    public async Task Types_that_cannot_be_voided_are_refused(string entity) =>
        await Assert.ThrowsAsync<McpException>(() => Tools().Void(entity, "1"));

    [Fact]
    public async Task Types_that_cannot_be_deleted_are_refused() =>
        await Assert.ThrowsAsync<McpException>(() => Tools().Delete("Transfer", "1"));

    [Fact]
    public async Task Delete_needs_the_delete_switch_and_the_entity_allowlist()
    {
        await Assert.ThrowsAsync<McpException>(() => Tools(WriteOp.Create | WriteOp.Update).Delete("Customer", "1"));
        await Assert.ThrowsAsync<McpException>(() => Tools(WriteOp.All, ["Invoice"]).Delete("Customer", "1"));
        Assert.Empty(_qb.Requests);
    }

    [Fact]
    public void Voidable_types_are_a_subset_of_deletable_ones()
    {
        var schema = WriteSchema.Instance;

        Assert.Subset(schema.TxnDelTypes.ToHashSet(), schema.TxnVoidTypes.ToHashSet());
        Assert.DoesNotContain("Estimate", schema.TxnVoidTypes);
        Assert.Contains("Estimate", schema.TxnDelTypes);
    }
}
