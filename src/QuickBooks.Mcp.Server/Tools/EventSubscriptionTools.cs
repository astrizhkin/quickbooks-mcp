using System.ComponentModel;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Event subscriptions that other applications registered with QuickBooks.</summary>
[McpServerToolType]
public sealed class EventSubscriptionTools(IQbClient qb)
{
    private static readonly string[] Kinds = ["DataEventSubscription", "UIEventSubscription", "UIExtensionSubscription", "DataEventRecoveryInfo"];

    /// <summary>Lists the subscriptions of one subscriber.</summary>
    [McpServerTool(Name = "get_event_subscriptions", ReadOnly = true)]
    [Description("Get the data event, UI event or UI extension subscriptions (or pending data event recovery info) registered by one subscriber application.")]
    public Task<string> GetEventSubscriptions(
        [Description("DataEventSubscription, UIEventSubscription, UIExtensionSubscription or DataEventRecoveryInfo.")] string kind,
        [Description("The subscriber's GUID, as the application registered it.")] string subscriberId,
        CancellationToken cancellationToken = default) =>
        ToolErrors.RunAsync(async () =>
        {
            var name = Kinds.FirstOrDefault(k => k.Equals(kind, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"kind must be one of {string.Join(", ", Kinds)}.");
            if (!Guid.TryParse(subscriberId, out var id))
                throw new ArgumentException($"'{subscriberId}' is not a GUID.");
            return QbXmlJson.ToJson((await qb.SendAsync(name + "QueryRq",
                [new XElement("SubscriberID", id.ToString("B").ToUpperInvariant())], cancellationToken)).Records);
        });
}
