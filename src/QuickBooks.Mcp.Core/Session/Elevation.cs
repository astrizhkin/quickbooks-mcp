using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Finds out whether QuickBooks and this program run at different Windows privilege levels
/// (one "as administrator", the other not), which makes the COM connection fail in confusing ways.
/// </summary>
public static class Elevation
{
    /// <summary>One running QuickBooks process. <see cref="Elevated"/> is null when it could not be inspected.</summary>
    public sealed record QuickBooksProcess(int Id, bool? Elevated);

    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenElevationClass = 20;

    /// <summary>True if this process has an elevated (administrator) token.</summary>
    public static bool IsCurrentProcessElevated() => IsProcessElevated(Environment.ProcessId) ?? false;

    /// <summary>The QuickBooks processes running for any user, with their elevation where it can be read.</summary>
    public static IReadOnlyList<QuickBooksProcess> FindQuickBooks() =>
        Process.GetProcessesByName("QBW32").Concat(Process.GetProcessesByName("QBW"))
            .Select(p => new QuickBooksProcess(p.Id, IsProcessElevated(p.Id))).ToList();

    /// <summary>
    /// Describes a privilege mismatch in plain words, or returns null if there is none (or QuickBooks is not running).
    /// A QuickBooks process that cannot be inspected from a non-elevated program is assumed elevated: Windows
    /// refuses access to a higher-privilege process's token.
    /// </summary>
    public static string? Describe(bool serverElevated, IEnumerable<QuickBooksProcess> quickBooks)
    {
        foreach (var qb in quickBooks)
        {
            var qbElevated = qb.Elevated ?? true;
            if (qbElevated && !serverElevated)
                return "QuickBooks is running as administrator but this program is not. Windows keeps them apart, so the connection can fail or hang. Start your AI app (or this command) as administrator too, or restart QuickBooks without \"Run as administrator\".";
            if (!qbElevated && serverElevated)
                return "This program is running as administrator but QuickBooks is not. Windows keeps them apart, so the connection can fail or hang. Start your AI app without \"Run as administrator\", or start QuickBooks as administrator.";
        }

        return null;
    }

    /// <summary>The elevation of <paramref name="processId"/>, or null if Windows will not say.</summary>
    public static bool? IsProcessElevated(int processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero) return null;
        try
        {
            if (!OpenProcessToken(process, TokenQuery, out var token)) return null;
            try
            {
                var size = sizeof(int);
                var buffer = Marshal.AllocHGlobal(size);
                try
                {
                    return GetTokenInformation(token, TokenElevationClass, buffer, size, out _) ? Marshal.ReadInt32(buffer) != 0 : null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CloseHandle(token);
            }
        }
        catch (Win32Exception)
        {
            return null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(IntPtr token, int informationClass, IntPtr information, int length, out int returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
