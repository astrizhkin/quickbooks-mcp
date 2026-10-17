using System.Runtime.InteropServices;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Cli;

/// <summary>The <c>--authorize</c> and <c>--doctor</c> commands.</summary>
public static class SetupCommands
{
    /// <summary>
    /// Opens a session so QuickBooks shows its access dialog, and explains how to answer it.
    /// </summary>
    public static async Task<int> AuthorizeAsync(QbOptions options, TextWriter output)
    {
        output.WriteLine("""
            Before continuing, in QuickBooks:
              1. Open the company file as a QuickBooks Admin user, in single-user mode.
              2. When the access dialog appears, choose "Yes, always; allow access even if QuickBooks is not running".
              3. Select an Admin user for the application and confirm.
            Connecting...
            """);
        return await ProbeAsync(options, output) ? 0 : 1;
    }

    /// <summary>Prints every check needed to diagnose a connection problem.</summary>
    public static async Task<int> DoctorAsync(QbOptions options, TextWriter output)
    {
        output.WriteLine($"Process bitness : {(Environment.Is64BitProcess ? "64" : "32")}-bit");
        output.WriteLine($"OS              : {RuntimeInformation.OSDescription}");
        var registered = OperatingSystem.IsWindows() && Type.GetTypeFromProgID("QBXMLRP2.RequestProcessor") is not null;
        output.WriteLine($"QBXMLRP2        : {(registered ? "registered" : "NOT registered (install QuickBooks Desktop; bitness must match)")}");

        var file = options.CompanyFile;
        output.WriteLine(string.IsNullOrEmpty(file)
            ? "Company file    : (none configured; the file open in QuickBooks will be used)"
            : $"Company file    : {file} ({(File.Exists(file) ? "found" : "NOT FOUND")})");

        var quickBooks = Elevation.FindQuickBooks();
        var serverElevated = Elevation.IsCurrentProcessElevated();
        output.WriteLine($"Run as admin    : {(serverElevated ? "yes" : "no")} (this program); QuickBooks: {(quickBooks.Count == 0 ? "not running" : string.Join(", ", quickBooks.Select(q => q.Elevated is null ? "unknown" : q.Elevated.Value ? "yes" : "no")))}");
        if (Elevation.Describe(serverElevated, quickBooks) is { } mismatch) output.WriteLine($"WARNING         : {mismatch}");

        if (!registered) return 1;
        return await ProbeAsync(options, output) ? 0 : 1;
    }

    private static async Task<bool> ProbeAsync(QbOptions options, TextWriter output)
    {
        try
        {
            var client = new LockingQbClient(new ComQbClient(options), options);
            var host = (await client.SendAsync("HostQueryRq")).Records.First();
            output.WriteLine($"Authorization   : OK");
            output.WriteLine($"Product         : {(string?)host.Element("ProductName")} {(string?)host.Element("MajorVersion")}.{(string?)host.Element("MinorVersion")}");
            output.WriteLine($"qbXML versions  : {string.Join(", ", host.Elements("SupportedQBXMLVersion").Select(e => e.Value))}");
            return true;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or OperationCanceledException)
        {
            output.WriteLine($"Authorization   : FAILED - {(ex is COMException com ? QbComErrors.Explain(com) : ex.Message)}");
            output.WriteLine("Run `--authorize` as an Admin in single-user mode, and check that no QuickBooks dialog is open.");
            return false;
        }
    }
}
