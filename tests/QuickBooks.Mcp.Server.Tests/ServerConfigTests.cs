using System.Collections;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tests;

public class ServerConfigTests
{
    private static Hashtable Env(params (string, string)[] pairs) =>
        new(pairs.ToDictionary(p => p.Item1, p => p.Item2));

    [Fact]
    public void Defaults_are_read_only()
    {
        var c = ServerConfig.Load([], Env());

        Assert.Equal(WriteOp.None, c.Writes);
        Assert.Null(c.Qb.CompanyFile);
        Assert.Equal(QbFileMode.DoNotCare, c.Qb.Mode);
    }

    [Fact]
    public void Cli_beats_env_beats_file()
    {
        var file = Path.GetTempFileName();
        File.WriteAllText(file, """{ "company_file": "file.qbw", "mode": "single", "max_retries": 5 }""");
        try
        {
            var c = ServerConfig.Load(["--config", file, "--company-file=cli.qbw", "--allow-writes"],
                Env(("QB_COMPANY_FILE", "env.qbw"), ("QB_MODE", "multiuser"), ("QB_MAX_RETRIES", "7")));

            Assert.Equal("cli.qbw", c.Qb.CompanyFile);
            Assert.Equal(QbFileMode.MultiUser, c.Qb.Mode);
            Assert.Equal(7, c.Qb.MaxRetries);
            Assert.Equal(WriteOp.All, c.Writes);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("QB_MODE", "weird")]
    [InlineData("QB_ALLOW_WRITES", "maybe")]
    [InlineData("QB_TIMEOUT_SECONDS", "0")]
    [InlineData("QB_LOG_LEVEL", "loud")]
    public void Invalid_values_give_readable_errors(string key, string value)
    {
        var ex = Assert.Throws<ConfigException>(() => ServerConfig.Load([], Env((key, value))));
        Assert.Contains(key[3..], ex.Message);
    }

    [Fact]
    public void Missing_config_file_is_an_error() =>
        Assert.Throws<ConfigException>(() => ServerConfig.Load(["--config", "nope.json"], Env()));
}

public class ModeAliasTests
{
    [Theory]
    [InlineData("any", QuickBooks.Mcp.Core.Session.QbFileMode.DoNotCare)]
    [InlineData("single", QuickBooks.Mcp.Core.Session.QbFileMode.SingleUser)]
    [InlineData("MULTI", QuickBooks.Mcp.Core.Session.QbFileMode.MultiUser)]
    [InlineData("MultiUser", QuickBooks.Mcp.Core.Session.QbFileMode.MultiUser)]
    public void Modes_accept_short_names(string text, QuickBooks.Mcp.Core.Session.QbFileMode expected) =>
        Assert.Equal(expected, ServerConfig.Load([], new System.Collections.Hashtable { ["QB_MODE"] = text }).Qb.Mode);
}
