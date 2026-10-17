using System.ComponentModel;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Payments waiting in Undeposited Funds.</summary>
[McpServerToolType]
public sealed class PaymentsToDepositTools(IQbClient qb)
{
    /// <summary>Lists payments received but not yet deposited.</summary>
    [McpServerTool(Name = "get_payments_to_deposit", ReadOnly = true)]
    [Description("Get payments and sales receipts received but not yet deposited (Undeposited Funds).")]
    public Task<string> GetPaymentsToDeposit(CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
            QbXmlJson.ToJson((await qb.SendAsync("ReceivePaymentToDepositQueryRq", [], cancellationToken)).Records));
}
