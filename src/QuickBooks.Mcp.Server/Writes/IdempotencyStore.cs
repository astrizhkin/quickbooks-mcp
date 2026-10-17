using System.Text.Json;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>
/// Remembers the outcome of creates that came with an idempotency key, so an agent that retries
/// after a timeout does not create a second invoice. Stored as JSON lines next to the audit log.
/// shortcut: remembers on this machine only and never expires; fine for one company file per config.
/// </summary>
public sealed class IdempotencyStore(string path)
{
    private sealed record Entry(string Key, string RequestHash, string Result);

    private readonly object _lock = new();

    /// <summary>
    /// Returns the stored result for <paramref name="key"/>, or null if the key is new.
    /// Throws if the key was used for a different request.
    /// </summary>
    public string? Find(string key, string requestHash)
    {
        lock (_lock)
        {
            if (!File.Exists(path)) return null;
            var entry = File.ReadLines(path).Select(l => JsonSerializer.Deserialize<Entry>(l)).FirstOrDefault(e => e?.Key == key);
            if (entry is null) return null;
            return entry.RequestHash == requestHash
                ? entry.Result
                : throw new InvalidOperationException($"Idempotency key '{key}' was already used for a different request. Use a new key for a new record.");
        }
    }

    /// <summary>Records the result of a completed create.</summary>
    public void Add(string key, string requestHash, string result)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, JsonSerializer.Serialize(new Entry(key, requestHash, result)) + Environment.NewLine);
        }
    }
}
