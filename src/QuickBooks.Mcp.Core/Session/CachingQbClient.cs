using System.Xml.Linq;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Remembers replies to queries for lists that rarely change (accounts, items, classes, terms, preferences) for a short time, so a
/// conversation that looks the same list up repeatedly does not open a QuickBooks session each time. Anything that is not a plain
/// query (every write) empties the cache, and a short lifetime bounds how stale it can get when someone edits QuickBooks directly.
/// </summary>
public sealed class CachingQbClient(IQbClient inner, TimeSpan lifetime, TimeProvider? time = null) : IQbClient
{
    private const int MaxEntries = 64;

    // shortcut: a fixed list; add names here when another list proves slow and stable.
    private static readonly HashSet<string> StableLists = new(StringComparer.Ordinal)
    {
        "AccountQueryRq", "BillingRateQueryRq", "ClassQueryRq", "CurrencyQueryRq", "CustomerMsgQueryRq", "CustomerTypeQueryRq",
        "DateDrivenTermsQueryRq", "JobTypeQueryRq", "PaymentMethodQueryRq", "PriceLevelQueryRq", "SalesRepQueryRq", "SalesTaxCodeQueryRq",
        "ShipMethodQueryRq", "StandardTermsQueryRq", "UnitOfMeasureSetQueryRq", "VehicleQueryRq", "VendorTypeQueryRq",
        "WorkersCompCodeQueryRq", "TemplateQueryRq", "PreferencesQueryRq", "CompanyQueryRq", "ItemQueryRq", "ItemServiceQueryRq",
        "ItemNonInventoryQueryRq", "ItemInventoryQueryRq", "ItemInventoryAssemblyQueryRq", "ItemOtherChargeQueryRq", "ItemDiscountQueryRq",
        "ItemSalesTaxQueryRq", "ItemSalesTaxGroupQueryRq", "ItemGroupQueryRq", "ItemSubtotalQueryRq", "ItemPaymentQueryRq", "ItemFixedAssetQueryRq",
    };

    private readonly Dictionary<string, (string Reply, DateTimeOffset Expires)> _entries = [];
    private readonly object _lock = new();

    private DateTimeOffset Now() => (time ?? TimeProvider.System).GetUtcNow();

    /// <summary>Forgets everything cached.</summary>
    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }

    /// <inheritdoc />
    public async Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        // The request text does not depend on the version in any way that matters for the key, so build it once with the newest.
        var key = buildRequest(QbXmlRequest.MaxSupportedVersion.ToString(2));
        var name = XDocument.Parse(key).Root?.Element("QBXMLMsgsRq")?.Elements().FirstOrDefault()?.Name.LocalName ?? "";
        var cacheable = lifetime > TimeSpan.Zero && StableLists.Contains(name);

        if (cacheable)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(key, out var hit) && hit.Expires > Now()) return hit.Reply;
            }
        }

        var isQuery = name.EndsWith("QueryRq", StringComparison.Ordinal);
        try
        {
            var reply = await inner.SendAsync(buildRequest, cancellationToken);
            if (cacheable)
            {
                lock (_lock)
                {
                    if (_entries.Count >= MaxEntries) _entries.Clear();
                    _entries[key] = (reply, Now() + lifetime);
                }
            }

            return reply;
        }
        finally
        {
            // A write that failed halfway may still have changed something, so the cache is emptied either way.
            if (!isQuery) Clear();
        }
    }
}
