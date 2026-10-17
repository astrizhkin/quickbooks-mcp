namespace QuickBooks.Mcp.Core.Session;

/// <summary>How the company file is opened.</summary>
public enum QbFileMode
{
    /// <summary>Whatever mode the file is already open in.</summary>
    DoNotCare = 2,

    /// <summary>Single-user mode.</summary>
    SingleUser = 0,

    /// <summary>Multi-user mode.</summary>
    MultiUser = 1,
}

/// <summary>Settings for talking to QuickBooks.</summary>
public sealed class QbOptions
{
    /// <summary>Application name shown in QuickBooks' authorized-applications list.</summary>
    public string AppName { get; init; } = "QuickBooks MCP";

    /// <summary>Company file to open; null or empty attaches to the file currently open in QuickBooks.</summary>
    public string? CompanyFile { get; init; }

    /// <summary>File mode used when opening <see cref="CompanyFile"/>.</summary>
    public QbFileMode Mode { get; init; } = QbFileMode.DoNotCare;

    /// <summary>
    /// Names the lock that makes calls take turns. QuickBooks has one company file open per computer, so the default is one lock
    /// for the whole machine; two servers configured for different files must not talk to QuickBooks at the same moment either.
    /// </summary>
    public string LockName { get; init; } = "default";

    /// <summary>Total time allowed per call, including waiting for the file lock and retries.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>How many times a failed COM call is retried (with backoff).</summary>
    public int MaxRetries { get; init; } = 3;
}
