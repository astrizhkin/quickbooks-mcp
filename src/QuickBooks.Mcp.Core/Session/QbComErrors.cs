using System.Runtime.InteropServices;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>Turns the COM errors QuickBooks raises into advice a person can act on.</summary>
public static class QbComErrors
{
    // shortcut: only the codes we are sure about; anything else gets QuickBooks' own text plus a generic hint.
    private static readonly Dictionary<uint, string> Hints = new()
    {
        [0x80040154] = "The QuickBooks request processor is not registered for this program's bitness. Install QuickBooks Desktop, and use the 32-bit (win-x86) build if QuickBooks only registers a 32-bit component.",
        [0x8004041A] = "This app has not been authorized for the company file. Open the file in QuickBooks as an admin, in single-user mode, and run --authorize.",
        [0x80040414] = "A dialog is open in QuickBooks. Close it (or finish what it asks) and try again.",
        [0x80040422] = "QuickBooks needs the company file in single-user mode, and another program is using it. Close the other program or switch the file to single-user mode.",
        [0x80040408] = "QuickBooks could not be started. Open QuickBooks and the company file first, then try again.",
    };

    /// <summary>A short explanation and next step for <paramref name="error"/>.</summary>
    public static string Explain(COMException error) =>
        Hints.TryGetValue(unchecked((uint)error.HResult), out var hint)
            ? $"{error.Message} {hint}"
            : $"{error.Message} If QuickBooks shows a dialog or is busy, deal with that and retry; run --doctor to check the setup.";
}
