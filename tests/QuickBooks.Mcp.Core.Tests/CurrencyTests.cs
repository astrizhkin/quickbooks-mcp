using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class CurrencyTests
{
    private sealed class ScriptedClient(Func<string, string> reply) : IQbClient
    {
        public List<string> Requests { get; } = [];

        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
        {
            var xml = buildRequest("16.0");
            var name = System.Xml.Linq.XDocument.Parse(xml).Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName;
            Requests.Add(name);
            return Task.FromResult(reply(name));
        }
    }

    private const string Preferences = """
        <QBXML><QBXMLMsgsRs><PreferencesQueryRs statusCode="0"><PreferencesRet>
          <MultiCurrencyPreferences><IsMultiCurrencyOn>true</IsMultiCurrencyOn><HomeCurrencyRef><FullName>US Dollar</FullName></HomeCurrencyRef></MultiCurrencyPreferences>
        </PreferencesRet></PreferencesQueryRs></QBXMLMsgsRs></QBXML>
        """;

    private static string Invoices(bool foreign) => $"""
        <QBXML><QBXMLMsgsRs><InvoiceQueryRs statusCode="0"><InvoiceRet>
          <TxnID>1</TxnID>{(foreign ? "<CurrencyRef><FullName>Euro</FullName></CurrencyRef><ExchangeRate>1.1</ExchangeRate>" : "")}<BalanceRemaining>100.00</BalanceRemaining>
        </InvoiceRet></InvoiceQueryRs></QBXMLMsgsRs></QBXML>
        """;

    [Fact]
    public async Task Single_currency_results_cost_no_extra_request_and_carry_no_note()
    {
        var qb = new ScriptedClient(_ => Invoices(foreign: false));

        var page = await new QueryRunner(qb).RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);

        Assert.Equal(["InvoiceQueryRq"], qb.Requests);
        Assert.Null(page["currency"]);
    }

    [Fact]
    public async Task Foreign_currency_results_state_the_home_currency_and_how_to_read_amounts()
    {
        var qb = new ScriptedClient(name => name == "PreferencesQueryRq" ? Preferences : Invoices(foreign: true));

        var page = await new QueryRunner(qb).RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);

        Assert.Equal("US Dollar", (string?)page["currency"]!["home"]);
        Assert.Contains("CurrencyRef", (string?)page["currency"]!["note"]);
        Assert.Contains("InHomeCurrency", (string?)page["currency"]!["note"]);
    }

    [Fact]
    public async Task The_home_currency_is_looked_up_once_per_session()
    {
        var qb = new ScriptedClient(name => name == "PreferencesQueryRq" ? Preferences : Invoices(foreign: true));
        var runner = new QueryRunner(qb);

        await runner.RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);
        await runner.RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);

        Assert.Single(qb.Requests, "PreferencesQueryRq");
    }

    [Fact]
    public async Task A_failed_preferences_lookup_still_returns_the_data_with_a_generic_note()
    {
        var qb = new ScriptedClient(name => name == "PreferencesQueryRq"
            ? """<QBXML><QBXMLMsgsRs><PreferencesQueryRs statusCode="3250" statusSeverity="Error" statusMessage="not available"/></QBXMLMsgsRs></QBXML>"""
            : Invoices(foreign: true));

        var page = await new QueryRunner(qb).RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);

        Assert.Equal(1, (int)page["count"]!);
        Assert.Null((string?)page["currency"]!["home"]);
        Assert.Contains("CurrencyRef", (string?)page["currency"]!["note"]);
    }

    [Fact]
    public async Task A_failed_lookup_is_retried_next_time()
    {
        var failures = 1;
        var qb = new ScriptedClient(name => name == "PreferencesQueryRq"
            ? failures-- > 0
                ? """<QBXML><QBXMLMsgsRs><PreferencesQueryRs statusCode="3250" statusSeverity="Error" statusMessage="busy"/></QBXMLMsgsRs></QBXML>"""
                : Preferences
            : Invoices(foreign: true));
        var runner = new QueryRunner(qb);

        await runner.RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);
        var second = await runner.RunAsync(Entity.Find("Invoice")!, new QueryFilters(), null, null);

        Assert.Equal("US Dollar", (string?)second["currency"]!["home"]);
    }
}
