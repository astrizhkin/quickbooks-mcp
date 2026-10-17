using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>The last payroll period; registered only with <c>--allow-payroll</c>.</summary>
[McpServerToolType]
public sealed class PayrollPeriodTools(IQbClient qb)
{
    /// <summary>Gets the dates of the last payroll period.</summary>
    [McpServerTool(Name = "get_payroll_last_period", ReadOnly = true)]
    [Description("Get the dates of the last payroll period that was run.")]
    public Task<string> GetPayrollLastPeriod(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("PayrollLastPeriodQueryRq", [], cancellationToken)).Records));
}
