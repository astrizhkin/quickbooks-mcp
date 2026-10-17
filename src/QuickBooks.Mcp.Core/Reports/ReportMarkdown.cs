using System.Text;
using System.Text.Json.Nodes;

namespace QuickBooks.Mcp.Core.Reports;

/// <summary>Renders a parsed report (see <see cref="ReportParser"/>) as a markdown table.</summary>
public static class ReportMarkdown
{
    /// <summary>Subtotal and total rows are bold; text rows are italic.</summary>
    public static string Render(JsonObject report)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine((string?)report["title"]);
        if ((string?)report["subtitle"] is { Length: > 0 } subtitle) sb.AppendLine(subtitle);
        if ((string?)report["basis"] is { Length: > 0 } basis) sb.Append("Basis: ").AppendLine(basis);
        sb.AppendLine();

        var columns = report["columns"]!.AsArray();
        var titles = columns.Select(c => Cell((string?)c!["title"] is { Length: > 0 } t ? t : (string?)c["type"])).ToList();
        sb.Append("| ").AppendJoin(" | ", titles).AppendLine(" |");
        sb.Append("| ").AppendJoin(" | ", columns.Select(c => (string?)c!["dataType"] is "text" ? "---" : "---:")).AppendLine(" |");

        foreach (var row in report["rows"]!.AsArray())
        {
            var kind = (string?)row!["kind"];
            var cells = row["cells"]!.AsArray().Select(c => Cell((string?)c)).Select(c => kind switch
            {
                "subtotal" or "total" when c.Length > 0 => $"**{c}**",
                "text" when c.Length > 0 => $"*{c}*",
                _ => c,
            });
            sb.Append("| ").AppendJoin(" | ", cells).AppendLine(" |");
        }

        return sb.ToString();
    }

    // Cell text comes from QuickBooks records anyone may have typed into, so it must not become links, images or HTML.
    private static string Cell(string? text) => (text ?? "")
        .Replace("\\", "\\\\").Replace("|", "\\|").Replace("[", "\\[").Replace("]", "\\]").Replace("`", "\\`")
        .Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");
}
