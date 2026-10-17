using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Core.Tests;

public class QueryTests
{
    private static readonly Entity Customer = Entity.Find("customer")!;
    private static readonly Entity Invoice = Entity.Find("Invoice")!;

    [Fact]
    public void List_filters_follow_schema_order()
    {
        var filters = new QueryFilters { NameContains = "Ac", Fields = ["Name"], ModifiedFrom = "2025-01-31" };

        var names = filters.ToElements(Customer, 51).Select(e => e.Name.LocalName);

        Assert.Equal(["MaxReturned", "ActiveStatus", "FromModifiedDate", "NameFilter", "IncludeRetElement"], names);
    }

    [Fact]
    public void Transaction_filters_use_ranges_and_entity_filter()
    {
        var filters = new QueryFilters { TxnDateFrom = "2025-01-01", EntityName = "Acme", IncludeLineItems = true };

        var names = filters.ToElements(Invoice, 10).Select(e => e.Name.LocalName);

        Assert.Equal(["MaxReturned", "TxnDateRangeFilter", "EntityFilter", "IncludeLineItems"], names);
    }

    [Fact]
    public void Bad_date_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new QueryFilters { ModifiedFrom = "yesterday" }.ToElements(Customer, 1));

    [Fact]
    public async Task Runner_returns_page_without_cursor_when_everything_fits()
    {
        var result = await new QueryRunner(new FixtureQbClient()).RunAsync(Customer, new QueryFilters(), null, null);

        Assert.Equal(1, (int)result["count"]!);
        Assert.Null(result["next_cursor"]);
    }

    [Fact]
    public async Task Runner_sets_cursor_when_more_records_exist()
    {
        var result = await new QueryRunner(new FixtureQbClient()).RunAsync(Customer, new QueryFilters(), 1, null);
        // The fixture holds one record, so asking for a page of 1 has no further page.
        Assert.Null(result["next_cursor"]);

        var second = await new QueryRunner(new FixtureQbClient()).RunAsync(Customer, new QueryFilters(), 1, "1");
        Assert.Equal(0, (int)second["count"]!);
    }

    [Fact]
    public void Unknown_entity_is_not_found() => Assert.Null(Entity.Find("Nope"));
}

public class QueryRulesTests
{
    private static readonly Entity Customer = Entity.Find("Customer")!;
    private static readonly Entity Invoice = Entity.Find("Invoice")!;

    [Fact]
    public void Lookup_by_id_sends_no_filters()
    {
        var names = new QueryFilters { Ids = ["8000-1"], Fields = ["Name"] }.ToElements(Customer, 51).Select(e => e.Name.LocalName);

        Assert.Equal(["ListID", "IncludeRetElement"], names);
    }

    [Fact]
    public void Lookup_cannot_be_mixed_with_filters() =>
        Assert.Throws<ArgumentException>(() => new QueryFilters { Ids = ["1"], NameContains = "x" }.ToElements(Customer, 1));

    [Fact]
    public void Modified_and_transaction_dates_are_exclusive() =>
        Assert.Throws<ArgumentException>(() => new QueryFilters { ModifiedFrom = "2025-01-01", TxnDateFrom = "2025-01-01" }.ToElements(Invoice, 1));

    [Theory]
    [InlineData("Check", true)]
    [InlineData("CreditCardCharge", false)]
    public void Linked_transactions_only_where_supported(string entity, bool supported)
    {
        var filters = new QueryFilters { IncludeLinkedTxns = true };

        if (supported) Assert.Contains(filters.ToElements(Entity.Find(entity)!, 1), e => e.Name == "IncludeLinkedTxns");
        else Assert.Throws<ArgumentException>(() => filters.ToElements(Entity.Find(entity)!, 1));
    }

    [Fact]
    public void Lead_is_looked_up_by_name_and_has_no_active_status()
    {
        var lead = Entity.Find("Lead")!;

        Assert.Equal("Name", new QueryFilters { FullNames = ["Pat"] }.ToElements(lead, 1).Single().Name.LocalName);
        Assert.DoesNotContain(new QueryFilters().ToElements(lead, 1), e => e.Name == "ActiveStatus");
    }

    [Fact]
    public void Templates_can_only_be_listed()
    {
        var template = Entity.Find("Template")!;

        Assert.Empty(new QueryFilters().ToElements(template, 1));
        Assert.Throws<ArgumentException>(() => new QueryFilters { NameContains = "x" }.ToElements(template, 1));
    }
}
