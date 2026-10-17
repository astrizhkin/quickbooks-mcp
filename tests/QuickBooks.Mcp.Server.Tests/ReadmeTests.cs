namespace QuickBooks.Mcp.Server.Tests;

public class ReadmeTests
{
    [Fact]
    public void The_readme_points_online_users_to_intuits_server_and_credits_the_design_source()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "README.md"))) dir = Path.GetDirectoryName(dir)!;
        var readme = File.ReadAllText(Path.Combine(dir, "README.md"));

        Assert.Contains("https://github.com/intuit/quickbooks-online-mcp-server", readme);
        Assert.Contains("Apache-2.0", readme);
        Assert.Contains("no code was copied", readme);
    }
}
