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
    /// Looking records up by id or full name cannot be combined with filters, and options a request
    /// does not support are refused rather than sent, because QuickBooks rejects the whole request.
    /// </summary>
    public List<XElement> ToElements(Entity entity, int maxReturned)
    {
        var e = new List<XElement>();
        var lookup = (Ids?.Count ?? 0) > 0 || (FullNames?.Count ?? 0) > 0;
        var hasFilter = Active is not null || NameContains is not null || NameStartsWith is not null || ModifiedFrom is not null
            || ModifiedTo is not null || TxnDateFrom is not null || TxnDateTo is not null || EntityName is not null || AccountName is not null;

        if (lookup && hasFilter) throw new ArgumentException("ids and fullNames cannot be combined with other filters; use one or the other.");
        if (lookup && (Ids?.Count ?? 0) > 0 && (FullNames?.Count ?? 0) > 0) throw new ArgumentException("Use ids or fullNames, not both.");
        if (entity.Unfilterable)
        {
            if (lookup || hasFilter) throw new ArgumentException($"{entity.Name} can only be listed; it has no filters.");
            return [.. (Fields ?? []).Select(f => new XElement("IncludeRetElement", f))];
        }

        if (IncludeLinkedTxns && !entity.HasLinkedTxns) throw new ArgumentException($"{entity.Name} does not support includeLinkedTxns.");
        if (IncludeLineItems && !entity.HasLineItems) throw new ArgumentException($"{entity.Name} does not support includeLineItems.");

        if (lookup)
        {
            e.AddRange((Ids ?? []).Select(id => new XElement(entity.IsTransaction ? "TxnID" : "ListID", id)));
            if (FullNames is { Count: > 0 })
                e.AddRange((entity.NameLookup is null ? throw new ArgumentException($"{entity.Name} cannot be looked up by name; use ids.") : FullNames)
                    .Select(n => new XElement(entity.NameLookup, n)));
        }
        else
        {
            e.Add(new XElement("MaxReturned", maxReturned));
            if (entity.IsTransaction) AddTransactionFilters(e, entity);
            else AddListFilters(e, entity);
        }

        if (entity.IsTransaction)
        {
            if (IncludeLineItems) e.Add(new XElement("IncludeLineItems", "true"));
            if (IncludeLinkedTxns) e.Add(new XElement("IncludeLinkedTxns", "true"));
        }

        e.AddRange((Fields ?? []).Select(f => new XElement("IncludeRetElement", f)));
        return e;
    }

    private void AddTransactionFilters(List<XElement> e, Entity entity)
    {
        // TransactionQuery is its own family: different element names, and both date filters may be combined.
        var all = entity.Name == "Transaction";
        var modified = ModifiedFrom is not null || ModifiedTo is not null;
        var dated = TxnDateFrom is not null || TxnDateTo is not null;
        if (modified && dated && !all) throw new ArgumentException("Filter by modified date or by transaction date, not both.");

        AddRange(e, all ? "TransactionModifiedDateRangeFilter" : "ModifiedDateRangeFilter", "FromModifiedDate", ModifiedFrom, "ToModifiedDate", ModifiedTo);
        AddRange(e, all ? "TransactionDateRangeFilter" : "TxnDateRangeFilter", "FromTxnDate", TxnDateFrom, "ToTxnDate", TxnDateTo);
        if (EntityName is not null)
        {
            if (!entity.HasEntityFilter) throw new ArgumentException($"{entity.Name} cannot be filtered by name.");
            var name = all ? "TransactionEntityFilter" : entity.Name == "TimeTracking" ? "TimeTrackingEntityFilter" : "EntityFilter";
            e.Add(new XElement(name, new XElement("FullName", EntityName)));
        }

        if (AccountName is not null)
        {
            if (!entity.HasAccountFilter) throw new ArgumentException($"{entity.Name} cannot be filtered by account.");
            e.Add(new XElement(all ? "TransactionAccountFilter" : "AccountFilter", new XElement("FullName", AccountName)));
        }

        if (Active is not null || NameContains is not null || NameStartsWith is not null)
            throw new ArgumentException("active, nameContains and nameStartsWith apply to list entities, not transactions.");
    }

    private void AddListFilters(List<XElement> e, Entity entity)
    {
        if (EntityName is not null || AccountName is not null || TxnDateFrom is not null || TxnDateTo is not null)
            throw new ArgumentException("Transaction filters (customer/vendor, account, transaction dates) apply to transactions, not lists.");

        if (entity.HasActiveStatus)
        {
            e.Add(new XElement("ActiveStatus", (Active ?? "active").ToLowerInvariant() switch
            {
                "active" => "ActiveOnly",
                "inactive" => "InactiveOnly",
                "all" => "All",
                var other => throw new ArgumentException($"active must be active, inactive or all, not '{other}'."),
            }));
        }
        else if (Active is not null)
        {
            throw new ArgumentException($"{entity.Name} cannot be filtered by active status.");
        }

        if (ModifiedFrom is not null) e.Add(new XElement("FromModifiedDate", Date(ModifiedFrom)));
        if (ModifiedTo is not null) e.Add(new XElement("ToModifiedDate", Date(ModifiedTo)));
        if (NameContains is not null) e.Add(NameFilter("Contains", NameContains));
        else if (NameStartsWith is not null) e.Add(NameFilter("StartsWith", NameStartsWith));
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
