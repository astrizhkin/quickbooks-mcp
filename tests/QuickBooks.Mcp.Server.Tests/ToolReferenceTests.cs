using QuickBooks.Mcp.Server.Cli;

namespace QuickBooks.Mcp.Server.Tests;

public class ToolReferenceTests
{
    private static string Committed()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "docs", "TOOLS.md"))) dir = Path.GetDirectoryName(dir)!;
        return File.ReadAllText(Path.Combine(dir, "docs", "TOOLS.md")).Replace("\r\n", "\n");
    }

    [Fact]
    public void The_committed_reference_matches_the_code() =>
        Assert.True(Committed() == ToolReference.Generate(),
            "docs/TOOLS.md is out of date. Regenerate it: dotnet run --project src/QuickBooks.Mcp.Server -- --tools-reference > docs/TOOLS.md");

    [Fact]
    public void Every_tool_has_an_example_prompt() =>
        Assert.All(ToolReference.AllTools().Select(t => t.ProtocolTool.Name), n => Assert.True(ToolExamples.Has(n), $"{n} needs an example in ToolExamples"));

    [Fact]
    public void Every_tool_and_parameter_is_described() =>
        Assert.All(ToolReference.AllTools(), t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.ProtocolTool.Description), $"{t.ProtocolTool.Name} has no description");
            foreach (var p in t.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject())
                Assert.True(p.Value.TryGetProperty("description", out _), $"{t.ProtocolTool.Name}.{p.Name} has no description");
        });
}
