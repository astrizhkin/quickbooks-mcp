using System.Text.Json.Nodes;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Reports;

/// <summary>Runs any report and returns it as a table.</summary>
public sealed class ReportRunner(IQbClient qb)
{
    /// <summary>Runs <paramref name="reportType"/> (any name from <see cref="ReportKind.All"/>).</summary>
    public async Task<JsonObject> RunAsync(string reportType, ReportOptions options, CancellationToken cancellationToken = default)
    {
        var (kind, type) = ReportKind.Find(reportType)
            ?? throw new ArgumentException($"Unknown report '{reportType}'. Use one of: {string.Join(", ", ReportKind.All.SelectMany(k => k.Types))}.");

        var response = await qb.SendAsync(kind.Request, options.ToElements(kind, type), cancellationToken);
        var report = response.Records.FirstOrDefault(r => r.Name.LocalName == "ReportRet")
            ?? throw new InvalidOperationException("QuickBooks returned no report.");
        return ReportParser.Parse(report);
    }
}
