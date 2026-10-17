using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Writes;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Removing event subscriptions that other applications registered.</summary>
[McpServerToolType]
public sealed class EventSubscriptionWriteTools(WriteExecutor executor)
{
    private static readonly string[] Kinds = ["Data", "UI", "UIExtension", "DataEventRecoveryInfo"];

    /// <summary>Deletes a subscriber's subscriptions of one kind.</summary>
    [McpServerTool(Name = "delete_event_subscription", ReadOnly = false, Destructive = true)]
    [Description("Delete the subscriptions of one subscriber application, or its pending data event recovery info. The user is asked to approve; If the client cannot ask the user, the first call returns a preview and a confirmation_token to pass on a second call.")]
    public Task<string> Delete(
        [Description("Data, UI, UIExtension or DataEventRecoveryInfo.")] string kind,
        [Description("The subscriber's GUID.")] string subscriberId,
        [Description("The confirmation_token from the preview; omit on the first call.")] string? confirmationToken = null,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(() =>
        {
            var name = Kinds.FirstOrDefault(k => k.Equals(kind, StringComparison.OrdinalIgnoreCase))
                ?? throw new McpException($"kind must be one of {string.Join(", ", Kinds)}.");
            if (!Guid.TryParse(subscriberId, out var id)) throw new McpException($"'{subscriberId}' is not a GUID.");
            var guid = new XElement("SubscriberID", id.ToString("B").ToUpperInvariant());
            var call = name == "DataEventRecoveryInfo"
                ? new WriteCall(WriteOp.Delete, name, "DataEventRecoveryInfoDelRq", [guid])
                : new WriteCall(WriteOp.Delete, name + "Subscription", "SubscriptionDelRq", [guid, new XElement("SubscriptionType", name)]);
            return executor.ExecuteAsync(call, false, confirmationToken, cancellationToken);
        });
}
