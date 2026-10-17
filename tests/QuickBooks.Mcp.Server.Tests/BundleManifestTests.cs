using System.Collections;
using System.Text.Json;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tests;

public class BundleManifestTests
{
    private static JsonElement Manifest()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "packaging", "mcpb", "manifest.json"))) dir = Path.GetDirectoryName(dir)!;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "packaging", "mcpb", "manifest.json"))).RootElement.Clone();
    }

    [Fact]
    public void Settings_screen_options_map_to_variables_the_server_reads()
    {
        var env = Manifest().GetProperty("server").GetProperty("mcp_config").GetProperty("env");
        var all = new Hashtable();
        foreach (var p in env.EnumerateObject()) all[p.Name] = p.Name == "QB_COMPANY_FILE" ? "C:/Books/Co.qbw" : p.Name == "QB_REDACT" ? "false" : "true";

        var config = ServerConfig.Load([], all);

        Assert.Equal("C:/Books/Co.qbw", config.Qb.CompanyFile);
        Assert.Equal(WriteOp.All, config.Writes);
        Assert.True(config.AllowPayroll);
        Assert.False(config.Redact);
    }

    [Fact]
    public void Defaults_in_the_manifest_are_the_safe_ones()
    {
        var options = Manifest().GetProperty("user_config");

        Assert.False(options.GetProperty("allow_writes").GetProperty("default").GetBoolean());
        Assert.False(options.GetProperty("allow_payroll").GetProperty("default").GetBoolean());
        Assert.True(options.GetProperty("redact").GetProperty("default").GetBoolean());
    }

    [Fact]
    public void An_empty_company_file_means_use_the_open_file()
    {
        var config = ServerConfig.Load([], new Hashtable { ["QB_COMPANY_FILE"] = "", ["QB_ALLOW_WRITES"] = "false" });

        Assert.True(string.IsNullOrEmpty(config.Qb.CompanyFile));
        Assert.Equal(WriteOp.None, config.Writes);
    }

    [Fact]
    public void The_bundle_is_windows_only() =>
        Assert.Equal("win32", Manifest().GetProperty("compatibility").GetProperty("platforms")[0].GetString());
}
