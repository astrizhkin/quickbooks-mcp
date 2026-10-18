using System.Security.Cryptography;
using System.Text;

namespace QuickBooks.Mcp.Server.Http;

/// <summary>The access key that every request in <c>--http</c> mode must present.</summary>
public static class HttpAccess
{
    /// <summary>Where the generated key is kept, next to the audit log.</summary>
    public static string KeyFilePath { get; } =
        Path.Combine(Path.GetDirectoryName(Writes.AuditLog.DefaultPath)!, "http-key.txt");

    /// <summary>The configured key, or the one in <paramref name="keyFile"/>; a new random key is created and saved when there is none.</summary>
    public static string ResolveKey(string? configured, string keyFile)
    {
        if (configured is not null) return configured;
        if (File.Exists(keyFile) && File.ReadAllText(keyFile).Trim() is { Length: >= 16 } saved) return saved;

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(keyFile))!);
        File.WriteAllText(keyFile, key);
        return key;
    }

    /// <summary>Whether the request carries the key as <c>Authorization: Bearer</c> or <c>X-API-Key</c>. The comparison takes the same time for every wrong key.</summary>
    public static bool IsAuthorized(string? authorization, string? apiKey, string key)
    {
        var presented = authorization is not null && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization[7..].Trim()
            : apiKey;
        return presented is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(key));
    }
}
