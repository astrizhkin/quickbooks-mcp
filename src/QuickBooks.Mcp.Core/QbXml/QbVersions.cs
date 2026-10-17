using System.Reflection;
using System.Text.Json;

namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>Raised when a request or field needs a newer qbXML version than the installed QuickBooks supports.</summary>
public sealed class UnsupportedVersionException(string message) : InvalidOperationException(message);

/// <summary>
/// Knows which qbXML version each request first appeared in (generated, see tools/write-schema), so an older
/// QuickBooks is told plainly what it cannot do instead of failing with a parser error.
/// </summary>
public static class QbVersions
{
    private static readonly Lazy<Dictionary<string, string>> Requests = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("qbxml-request-versions.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    });

    /// <summary>True if qbXML version <paramref name="have"/> is at least <paramref name="need"/> (e.g. "12.0" vs "8.0").</summary>
    public static bool AtLeast(string have, string need) => Version.Parse(have) >= Version.Parse(need);

    /// <summary>Throws if <paramref name="request"/> (e.g. <c>InventorySiteQueryRq</c>) is newer than <paramref name="version"/>.</summary>
    public static void EnsureRequest(string request, string version)
    {
        if (Requests.Value.TryGetValue(request, out var since) && !AtLeast(version, since))
            throw new UnsupportedVersionException(
                $"{request} needs qbXML {since} or newer, and this QuickBooks supports up to {version}. That feature is not available in this QuickBooks version.");
    }

    /// <summary>Throws if any field in <paramref name="needs"/> (path, first version) is newer than <paramref name="version"/>.</summary>
    public static void EnsureFields(IEnumerable<(string Path, string Since)> needs, string version)
    {
        var tooNew = needs.Where(n => !AtLeast(version, n.Since)).ToList();
        if (tooNew.Count > 0)
            throw new UnsupportedVersionException(
                $"This QuickBooks supports up to qbXML {version}, but {string.Join(", ", tooNew.Select(n => $"{n.Path} (needs {n.Since})"))} " +
                "are newer. Leave those fields out.");
    }
}
