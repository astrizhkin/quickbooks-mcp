using System.Runtime.InteropServices;
using System.Text;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Cli;

/// <summary>The <c>--diagnostics</c> command: a report that is safe to paste into a bug report.</summary>
public static class DiagnosticsCommand
{
    /// <summary>
    /// Writes versions, environment, settings and the <c>--doctor</c> checks. It contains no company data: the company
    /// file is shown by file name only, and nothing is read from the company file beyond the product name and versions.
    /// </summary>
    public static async Task<int> RunAsync(ServerConfig config, TextWriter output)
    {
        output.WriteLine("quickbooks-mcp diagnostics");
        output.WriteLine($"Server version  : {McpSetup.Version}");
        output.WriteLine($"Time            : {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        output.WriteLine($".NET runtime    : {RuntimeInformation.FrameworkDescription}");
        output.WriteLine($"Architecture    : {RuntimeInformation.ProcessArchitecture}");
        output.WriteLine();
        output.WriteLine("Settings (company file shown by name only)");
        output.WriteLine($"  company file  : {(string.IsNullOrEmpty(config.Qb.CompanyFile) ? "(open file)" : Path.GetFileName(config.Qb.CompanyFile))}");
        output.WriteLine($"  mode          : {config.Qb.Mode}, timeout {config.Qb.Timeout.TotalSeconds:0}s, retries {config.Qb.MaxRetries}");
        output.WriteLine($"  writes        : {config.Writes}{(config.WriteAllow is null ? "" : " limited to " + string.Join(",", config.WriteAllow))}");
        output.WriteLine($"  redaction     : {(config.Redact ? "on" : "off")}, payroll {(config.AllowPayroll ? "allowed" : "off")}");
        output.WriteLine($"  log level     : {config.LogLevel}{(config.LogFile is null ? "" : ", file " + Path.GetFileName(config.LogFile))}");
        output.WriteLine();
        output.WriteLine("Checks");

        var checks = new StringWriter();
        var code = await SetupCommands.DoctorAsync(config.Qb, checks);
        var text = checks.ToString();
        if (!string.IsNullOrEmpty(config.Qb.CompanyFile)) text = text.Replace(config.Qb.CompanyFile, Path.GetFileName(config.Qb.CompanyFile));
        foreach (var line in text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            output.WriteLine("  " + line);

        if (config.LogFile is { } log && File.Exists(log))
        {
            output.WriteLine();
            output.WriteLine("Recent log lines");
            foreach (var line in File.ReadLines(log).TakeLast(20)) output.WriteLine("  " + line);
        }

        return code;
    }
}
