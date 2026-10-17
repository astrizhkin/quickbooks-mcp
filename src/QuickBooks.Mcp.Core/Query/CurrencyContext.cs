using System.Runtime.CompilerServices;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Query;

/// <summary>
/// The company's home currency, read from the preferences once and remembered. Only looked up when a result
/// actually contains a foreign currency (a <c>CurrencyRef</c>), so single-currency companies pay nothing.
/// </summary>
public sealed class CurrencyContext(IQbClient qb)
{
    private static readonly ConditionalWeakTable<IQbClient, CurrencyContext> Cache = [];
    private Task<string?>? _home;

    /// <summary>The context shared by everything that uses <paramref name="qb"/>.</summary>
    public static CurrencyContext For(IQbClient qb) => Cache.GetValue(qb, c => new CurrencyContext(c));

    /// <summary>The home currency name (e.g. "US Dollar"), or null if QuickBooks did not say.</summary>
    public Task<string?> HomeCurrencyAsync(CancellationToken cancellationToken = default)
    {
        // A failed lookup is not remembered, so a busy QuickBooks does not hide the currency for the rest of the session.
        var task = _home ??= Read(cancellationToken);
        if (task.IsFaulted || task.IsCanceled) _home = null;
        return task;
    }

    private async Task<string?> Read(CancellationToken cancellationToken)
    {
        var preferences = (await qb.SendAsync("PreferencesQueryRq", cancellationToken: cancellationToken)).Records.FirstOrDefault();
        return (string?)preferences?.Element("MultiCurrencyPreferences")?.Element("HomeCurrencyRef")?.Element("FullName");
    }

    /// <summary>Plain-words guidance for a result that holds foreign-currency amounts.</summary>
    public static string Note(string? home) =>
        (home is null ? "" : $"The home currency is {home}. ") +
        "Amounts on a record are in that record's own CurrencyRef currency unless the field name ends in InHomeCurrency, which is in the home currency. " +
        "Reports are in the home currency.";
}
