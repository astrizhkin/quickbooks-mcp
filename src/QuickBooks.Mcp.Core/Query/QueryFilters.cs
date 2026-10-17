using System.Globalization;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.Query;

/// <summary>Filters accepted by a query, turned into the ordered children of a <c>*QueryRq</c>.</summary>
public sealed class QueryFilters
{
    /// <summary>ListID values (list entities) or TxnID values (transactions).</summary>
    public IReadOnlyList<string>? Ids { get; init; }

    /// <summary>Exact FullName values (list entities only).</summary>
    public IReadOnlyList<string>? FullNames { get; init; }

    /// <summary>"active" (default), "inactive" or "all" (list entities only).</summary>
    public string? Active { get; init; }

    /// <summary>Name contains this text (list entities only).</summary>
    public string? NameContains { get; init; }

    /// <summary>Name starts with this text (list entities only).</summary>
    public string? NameStartsWith { get; init; }

    /// <summary>Last-modified range start (yyyy-MM-dd).</summary>
    public string? ModifiedFrom { get; init; }

    /// <summary>Last-modified range end (yyyy-MM-dd).</summary>
    public string? ModifiedTo { get; init; }

    /// <summary>Transaction date range start (transactions only).</summary>
    public string? TxnDateFrom { get; init; }

    /// <summary>Transaction date range end (transactions only).</summary>
    public string? TxnDateTo { get; init; }

    /// <summary>Customer, vendor or employee full name on the transaction (transactions only).</summary>
    public string? EntityName { get; init; }

    /// <summary>Account full name on the transaction (transactions only).</summary>
    public string? AccountName { get; init; }

    /// <summary>Include transaction lines (transactions only).</summary>
    public bool IncludeLineItems { get; init; }

    /// <summary>Include linked transactions (transactions only).</summary>
    public bool IncludeLinkedTxns { get; init; }

    /// <summary>Only return these fields (IncludeRetElement).</summary>
    public IReadOnlyList<string>? Fields { get; init; }

    /// <summary>
    /// Builds the request children in the order the qbXML schema requires.
    /// <paramref name="maxReturned"/> is the number of records to ask QuickBooks for.
    /// </summary>
    public List<XElement> ToElements(Entity entity, int maxReturned)
    {
        var e = new List<XElement>();
        e.AddRange((Ids ?? []).Select(id => new XElement(entity.IsTransaction ? "TxnID" : "ListID", id)));
        if (!entity.IsTransaction) e.AddRange((FullNames ?? []).Select(n => new XElement("FullName", n)));
        e.Add(new XElement("MaxReturned", maxReturned));

        if (entity.IsTransaction)
        {
            AddRange(e, "ModifiedDateRangeFilter", "FromModifiedDate", ModifiedFrom, "ToModifiedDate", ModifiedTo);
            AddRange(e, "TxnDateRangeFilter", "FromTxnDate", TxnDateFrom, "ToTxnDate", TxnDateTo);
            if (EntityName is not null) e.Add(new XElement("EntityFilter", new XElement("FullName", EntityName)));
            if (AccountName is not null) e.Add(new XElement("AccountFilter", new XElement("FullName", AccountName)));
            if (IncludeLineItems) e.Add(new XElement("IncludeLineItems", "true"));
            if (IncludeLinkedTxns) e.Add(new XElement("IncludeLinkedTxns", "true"));
        }
        else
        {
            e.Add(new XElement("ActiveStatus", (Active ?? "active").ToLowerInvariant() switch
            {
                "active" => "ActiveOnly",
                "inactive" => "InactiveOnly",
                "all" => "All",
                var other => throw new ArgumentException($"active must be active, inactive or all, not '{other}'."),
            }));
            if (ModifiedFrom is not null) e.Add(new XElement("FromModifiedDate", Date(ModifiedFrom)));
            if (ModifiedTo is not null) e.Add(new XElement("ToModifiedDate", Date(ModifiedTo)));
            if (NameContains is not null) e.Add(NameFilter("Contains", NameContains));
            else if (NameStartsWith is not null) e.Add(NameFilter("StartsWith", NameStartsWith));
        }

        e.AddRange((Fields ?? []).Select(f => new XElement("IncludeRetElement", f)));
        return e;
    }

    private static XElement NameFilter(string criterion, string name) =>
        new("NameFilter", new XElement("MatchCriterion", criterion), new XElement("Name", name));

    private static void AddRange(List<XElement> into, string wrapper, string fromName, string? from, string toName, string? to)
    {
        if (from is null && to is null) return;
        var range = new XElement(wrapper);
        if (from is not null) range.Add(new XElement(fromName, Date(from)));
        if (to is not null) range.Add(new XElement(toName, Date(to)));
        into.Add(range);
    }

    private static string Date(string text) =>
        DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var d)
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : throw new ArgumentException($"'{text}' is not a date; use yyyy-MM-dd.");
}
