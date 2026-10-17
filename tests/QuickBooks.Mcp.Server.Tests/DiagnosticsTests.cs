using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Cli;
using QuickBooks.Mcp.Server.Diagnostics;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class DiagnosticsTests
{
    private sealed class CapturingLogger : ILogger<LoggingQbClient>
    {
        public List<(LogLevel Level, string Text)> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add((logLevel, formatter(state, exception)));
    }

    private sealed class FailingClient(Exception error) : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            buildRequest("16.0");
            throw error;
        }
    }

    [Fact]
    public async Task Calls_are_logged_by_request_name_without_any_company_data()
    {
        var log = new CapturingLogger();
        var client = new LoggingQbClient(new FixtureQbClient(), log);

        await client.SendAsync("CustomerQueryRq", [new System.Xml.Linq.XElement("FullName", "Secret Customer Ltd")]);

        var line = Assert.Single(log.Lines);
        Assert.Contains("CustomerQueryRq", line.Text);
        Assert.DoesNotContain("Secret Customer", line.Text);
        Assert.DoesNotContain("Example Customer", line.Text);
    }

    [Fact]
    public async Task Failures_log_the_kind_and_code_and_keep_the_message_for_debug_only()
    {
        var log = new CapturingLogger();
        var client = new LoggingQbClient(new FailingClient(new COMException("Cannot open C:\\Users\\pat\\Secret Books.qbw", unchecked((int)0x8004041A))), log);

        await Assert.ThrowsAsync<COMException>(() => client.SendAsync(v => QuickBooks.Mcp.Core.QbXml.QbXmlRequest.Build(v, "HostQueryRq")));

        var warning = log.Lines.Single(l => l.Level == LogLevel.Warning);
        Assert.Contains("0x8004041A", warning.Text);
        Assert.Contains("HostQueryRq", warning.Text);
        Assert.DoesNotContain("Secret Books", warning.Text);
        Assert.Contains(log.Lines, l => l.Level == LogLevel.Debug && l.Text.Contains("Secret Books"));
    }

    [Fact]
    public void The_file_logger_appends_lines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qbmcp-{Guid.NewGuid()}", "server.log");
        try
        {
            using (var provider = new FileLoggerProvider(path))
            {
                var logger = provider.CreateLogger("test");
                logger.LogInformation("first {Name}", "one");
                logger.LogWarning("second");
            }

            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("first one", lines[0]);
            Assert.Contains("Warning", lines[1]);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void The_log_file_setting_is_read()
    {
        var config = ServerConfig.Load(["--log-file", "C:/logs/qb.log"], new Hashtable());

        Assert.Equal("C:/logs/qb.log", config.LogFile);
    }

    [Fact]
    public async Task The_diagnostics_report_shows_versions_and_checks_but_not_paths()
    {
        var config = ServerConfig.Load([], new Hashtable { ["QB_COMPANY_FILE"] = "C:/Users/pat/Documents/Secret Books.qbw" });
        var output = new StringWriter();

        await DiagnosticsCommand.RunAsync(config, output);
        var text = output.ToString();

        Assert.Contains("Server version", text);
        Assert.Contains("Secret Books.qbw", text);
        Assert.DoesNotContain("pat", text.Replace("path", ""));
        Assert.Contains("Checks", text);
        Assert.Contains("QBXMLRP2", text);
    }
}
