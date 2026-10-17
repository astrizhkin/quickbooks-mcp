using ModelContextProtocol;

namespace QuickBooks.Mcp.Server.Tests;

internal static class ToolTestHelpers
{
    /// <summary>The names of the elements sent inside the first request.</summary>
    public static IEnumerable<string> SentElements(this QuickBooks.Mcp.TestSupport.FixtureQbClient qb) =>
        qb.Requests[0].Root!.Element("QBXMLMsgsRq")!.Elements().First().Elements().Select(e => e.Name.LocalName);

    public static string SentRequest(this QuickBooks.Mcp.TestSupport.FixtureQbClient qb) =>
        qb.Requests[0].Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName;

    public static Task<McpException> RejectsAsync(Func<Task> action) => Assert.ThrowsAsync<McpException>(action);
}
