using System.Text.Json;
using ModelContextProtocol;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public sealed class VersionHandlingTests : IDisposable
{
    private readonly string _log = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid()}.jsonl");

    public void Dispose() => File.Delete(_log);

    /// <summary>A QuickBooks that only understands up to a given qbXML version.</summary>
    private sealed class OldQuickBooks(string version) : IQbClient
    {
        public List<string> Sent { get; } = [];

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            Sent.Add(buildRequest(version));
            return Task.FromResult("""<QBXML><QBXMLMsgsRs><CustomerAddRs statusCode="0"><CustomerRet><ListID>1</ListID></CustomerRet></CustomerAddRs></QBXMLMsgsRs></QBXML>""");
        }
    }

    [Theory]
    [InlineData("InventorySiteQueryRq", "9.0", false)]
    [InlineData("InventorySiteQueryRq", "10.0", true)]
    [InlineData("InventorySiteQueryRq", "16.0", true)]
    [InlineData("CustomerQueryRq", "2.0", true)]
    [InlineData("NotARequestRq", "2.0", true)]
    public void Requests_newer_than_the_installed_version_are_refused(string request, string version, bool ok)
    {
        if (ok) QbVersions.EnsureRequest(request, version);
        else Assert.Throws<UnsupportedVersionException>(() => QbVersions.EnsureRequest(request, version));
    }

    [Fact]
    public void Versions_compare_numerically_not_as_text()
    {
        Assert.True(QbVersions.AtLeast("10.0", "9.0"));
        Assert.False(QbVersions.AtLeast("9.0", "10.0"));
    }

    [Fact]
    public async Task A_query_for_a_newer_feature_says_which_version_it_needs()
    {
        var old = new OldQuickBooks("9.0");

        var ex = await Assert.ThrowsAsync<UnsupportedVersionException>(() => old.SendAsync("ItemSitesQueryRq"));

        Assert.Contains("10.0", ex.Message);
        Assert.Contains("9.0", ex.Message);
        Assert.Empty(old.Sent);
    }

    [Fact]
    public async Task Tools_report_it_as_a_tool_error()
    {
        var old = new OldQuickBooks("9.0");

        var ex = await Assert.ThrowsAsync<McpException>(() => new InventoryTools(old).GetItemSites(itemType: "Inventory"));

        Assert.Contains("ItemSitesQueryRq", ex.Message);
    }

    [Fact]
    public async Task A_field_newer_than_the_installed_version_is_refused_by_name_and_nothing_is_sent()
    {
        var old = new OldQuickBooks("8.0");
        var tools = new ListWriteTools(new RecordWriter(old, new WriteExecutor(old, new WriteGate(WriteOp.All), new AuditLog(_log))));

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            tools.Create("Customer", JsonDocument.Parse("""{ "Name": "A", "ClassRef": { "FullName": "East" } }""").RootElement));

        Assert.Contains("CustomerAdd/ClassRef", ex.Message);
        Assert.Contains("12.0", ex.Message);
        Assert.Empty(old.Sent);
    }

    [Fact]
    public async Task The_same_request_goes_through_on_a_new_enough_quickbooks()
    {
        var recent = new OldQuickBooks("16.0");
        var tools = new ListWriteTools(new RecordWriter(recent, new WriteExecutor(recent, new WriteGate(WriteOp.All), new AuditLog(_log))));

        await tools.Create("Customer", JsonDocument.Parse("""{ "Name": "A", "ClassRef": { "FullName": "East" } }""").RootElement);

        Assert.Single(recent.Sent);
    }

    [Fact]
    public void Field_listings_mark_what_needs_a_newer_version()
    {
        var tree = WriteSchema.Instance.Describe("Customer", "Add");

        Assert.Equal("qbXML 12.0+", (string?)tree["ClassRef"]!["_needs"]);
    }

    [Fact]
    public void The_generated_tables_know_a_few_well_known_cases()
    {
        QbVersions.EnsureRequest("LeadQueryRq", "13.0");
        Assert.Throws<UnsupportedVersionException>(() => QbVersions.EnsureRequest("LeadQueryRq", "12.0"));
    }
}
