namespace QuickBooks.Mcp.Server.Tests;

public class SiteTests
{
    private static string Root()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "site", "index.html"))) dir = Path.GetDirectoryName(dir)!;
        return dir;
    }

    [Fact]
    public void The_site_states_the_same_install_commands_as_the_readme()
    {
        var site = File.ReadAllText(Path.Combine(Root(), "site", "index.html"));
        var readme = File.ReadAllText(Path.Combine(Root(), "README.md"));

        foreach (var command in new[] { "npx -y @devwithmj/quickbooks-mcp", "dnx DevWithMJ.QuickBooks.Mcp --yes" })
        {
            Assert.Contains(command, site);
            Assert.Contains(command, readme);
        }
    }

    [Fact]
    public void The_site_works_without_scripts_or_remote_assets_and_says_it_is_not_affiliated()
    {
        var site = File.ReadAllText(Path.Combine(Root(), "site", "index.html"));

        Assert.DoesNotContain("<script", site);
        Assert.DoesNotContain("src=\"http", site);
        Assert.DoesNotContain("href=\"http://", site);
        Assert.Contains("Not affiliated with or endorsed by Intuit", site);
        Assert.True(File.Exists(Path.Combine(Root(), "site", "icon.svg")));
    }
}
