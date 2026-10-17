using System.Text.Json;

namespace QuickBooks.Mcp.Server.Tests;

public class RegistryMetadataTests
{
    private static string Root()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "server.json"))) dir = Path.GetDirectoryName(dir)!;
        return dir;
    }

    private static JsonElement Json(params string[] path) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine([Root(), .. path]))).RootElement.Clone();

    [Fact]
    public void One_server_name_everywhere()
    {
        var name = Json("server.json").GetProperty("name").GetString();

        Assert.Equal("io.github.devwithmj/quickbooks-mcp", name);
        Assert.Equal(name, Json("packaging", "server.json").GetProperty("name").GetString());
        Assert.Equal(name, Json("packaging", "npm", "package.json").GetProperty("mcpName").GetString());
        Assert.Contains($"mcp-name: {name} ", File.ReadAllText(Path.Combine(Root(), "README.md")) + " ");
    }

    [Fact]
    public void Registry_file_lists_the_published_packages_with_placeholders_for_the_version()
    {
        var server = Json("server.json");
        var packages = server.GetProperty("packages").EnumerateArray().ToList();

        Assert.Equal("__VERSION__", server.GetProperty("version").GetString());
        Assert.Equal(["npm", "nuget"], packages.Select(p => p.GetProperty("registryType").GetString()));
        Assert.Equal(["@devwithmj/quickbooks-mcp", "DevWithMJ.QuickBooks.Mcp"], packages.Select(p => p.GetProperty("identifier").GetString()));
        Assert.All(packages, p => Assert.Equal("__VERSION__", p.GetProperty("version").GetString()));
        Assert.All(packages, p => Assert.Equal("stdio", p.GetProperty("transport").GetProperty("type").GetString()));
    }

    [Fact]
    public void Descriptions_agree_and_fit_the_registry_limit()
    {
        var description = Json("server.json").GetProperty("description").GetString()!;

        Assert.Equal(description, Json("packaging", "server.json").GetProperty("description").GetString());
        Assert.InRange(description.Length, 1, 100);
    }
}
