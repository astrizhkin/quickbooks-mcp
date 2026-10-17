using System.Text.Json;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>
/// Append-only JSON-lines record of every write. It stores what happened, when, and which
/// record IDs were touched, never the request or response data, so it holds no personal data.
/// </summary>
public sealed class AuditLog(string path)
{
    private readonly object _lock = new();

    /// <summary>Default location: <c>%LOCALAPPDATA%\quickbooks-mcp\audit.jsonl</c>.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "quickbooks-mcp", "audit.jsonl");

    /// <summary>Appends one entry.</summary>
    public void Append(string outcome, WriteOp op, string entity, string requestHash, IEnumerable<string> ids, string? error = null)
    {
        var line = JsonSerializer.Serialize(new
        {
            time = DateTimeOffset.UtcNow,
            outcome,
            op = op.ToString(),
            entity,
            requestHash,
            ids,
            error,
        });
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
