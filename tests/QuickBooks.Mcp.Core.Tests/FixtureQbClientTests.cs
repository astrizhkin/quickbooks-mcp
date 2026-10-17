using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Core.Tests;

public class FixtureQbClientTests
{
    [Fact]
    public async Task Answers_with_matching_fixture_and_records_request()
    {
        var client = new FixtureQbClient();

        var response = await client.SendAsync("CustomerQueryRq");

        Assert.Equal("Example Customer", (string?)response.Records.Single().Element("Name"));
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task Unknown_request_fails_loudly() =>
        await Assert.ThrowsAsync<FileNotFoundException>(() => new FixtureQbClient().SendAsync("VehicleQueryRq"));
}
