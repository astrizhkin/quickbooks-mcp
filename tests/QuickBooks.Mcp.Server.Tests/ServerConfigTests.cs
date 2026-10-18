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
    public void Http_is_off_by_default_and_uses_a_loopback_port()
    {
        var c = ServerConfig.Load([], Env());
        var on = ServerConfig.Load(["--http", "--http-port", "9000"], Env());

        Assert.False(c.Http);
        Assert.Equal(8765, c.HttpPort);
        Assert.True(on.Http);
        Assert.Equal(9000, on.HttpPort);
    }

    [Fact]
    public void Http_refuses_changes_unless_they_are_allowed_for_http_too()
    {
        Assert.Throws<ConfigException>(() => ServerConfig.Load(["--http", "--allow-writes"], Env()));
        Assert.Equal(WriteOp.All, ServerConfig.Load(["--http", "--allow-writes", "--http-allow-writes"], Env()).Writes);
        Assert.Equal(WriteOp.All, ServerConfig.Load(["--allow-writes"], Env()).Writes);
    }

    [Fact]
    public void A_short_http_key_is_refused()
    {
        Assert.Throws<ConfigException>(() => ServerConfig.Load(["--http"], Env(("QB_HTTP_KEY", "short"))));
        Assert.Equal("0123456789abcdef", ServerConfig.Load(["--http"], Env(("QB_HTTP_KEY", "0123456789abcdef"))).HttpKey);
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

public class CacheSettingTests
{
    [Fact]
    public void The_cache_defaults_to_thirty_seconds_and_can_be_turned_off()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), ServerConfig.Load([], new System.Collections.Hashtable()).CacheLifetime);
        Assert.Equal(TimeSpan.Zero, ServerConfig.Load([], new System.Collections.Hashtable { ["QB_CACHE_SECONDS"] = "0" }).CacheLifetime);
        Assert.Throws<ConfigException>(() => ServerConfig.Load([], new System.Collections.Hashtable { ["QB_CACHE_SECONDS"] = "-5" }));
    }
}
