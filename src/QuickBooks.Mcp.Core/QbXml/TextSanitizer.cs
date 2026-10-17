using System.Globalization;
using System.Text;

namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>
/// Cleans text that other people typed into QuickBooks before it reaches the model: names, memos and
/// descriptions are untrusted. Removes characters a person cannot see (so instructions cannot be hidden in
/// them) and caps the length of any single value.
/// </summary>
public static class TextSanitizer
{
    /// <summary>The most characters one value may keep.</summary>
    public const int MaxLength = 4000;

    /// <summary>Returns <paramref name="text"/> without invisible or control characters, cut to <see cref="MaxLength"/>.</summary>
    public static string Clean(string text)
    {
        // Fast path: ordinary text needs no copy.
        if (text.Length <= MaxLength && text.EnumerateRunes().All(Keep)) return text;

        var sb = new StringBuilder(Math.Min(text.Length, MaxLength));
        var kept = 0;
        var dropped = 0;
        foreach (var rune in text.EnumerateRunes().Where(Keep))
        {
            if (kept >= MaxLength) { dropped++; continue; }
            sb.Append(rune.ToString());
            kept += rune.Utf16SequenceLength;
        }

        if (dropped > 0) sb.Append($" ... [truncated, {dropped} more characters]");
        return sb.ToString();
    }

    private static bool Keep(Rune rune)
    {
        if (rune.Value is '\t' or '\n' or '\r' or 0x200C or 0x200D) return true;
        return Rune.GetUnicodeCategory(rune) is not (
            UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.PrivateUse or
            UnicodeCategory.OtherNotAssigned or UnicodeCategory.Surrogate);
    }
}
