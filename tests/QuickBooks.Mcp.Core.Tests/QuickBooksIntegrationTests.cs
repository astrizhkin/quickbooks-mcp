using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

/// <summary>Runs only when <c>QB_COMPANY_FILE</c> is set (ideally to Intuit's sample company file).</summary>
public sealed class RequiresQuickBooksFactAttribute : FactAttribute
{
    public RequiresQuickBooksFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QB_COMPANY_FILE")))
            Skip = "Set QB_COMPANY_FILE to run integration tests.";
    }
}

/// <summary>
/// Read-only smoke tests against a real QuickBooks, used to fill in docs/COMPATIBILITY.md.
/// Authorize the app first (<c>--authorize</c>) and open the file in the mode you want to test.
/// </summary>
public class QuickBooksIntegrationTests
{
    private static IQbClient Client()
    {
        var options = new QbOptions { CompanyFile = Environment.GetEnvironmentVariable("QB_COMPANY_FILE") };
        return new LockingQbClient(new ComQbClient(options), options);
    }

    [RequiresQuickBooksFact]
    public async Task Host_query_returns_product_name()
    {
        var response = await Client().SendAsync("HostQueryRq");

        Assert.False(string.IsNullOrEmpty((string?)response.Records.First().Element("ProductName")));
    }

    [RequiresQuickBooksFact]
    public async Task Company_and_preferences_can_be_read()
    {
        var client = Client();

        Assert.NotEmpty((await client.SendAsync("CompanyQueryRq")).Records);
        Assert.NotEmpty((await client.SendAsync("PreferencesQueryRq")).Records);
    }

    [RequiresQuickBooksFact]
    public async Task The_common_lists_can_be_queried()
    {
        var runner = new QueryRunner(Client());

        foreach (var name in new[] { "Account", "Customer", "Vendor", "Item", "Class" })
        {
            var page = await runner.RunAsync(Entity.Find(name)!, new QueryFilters(), 5, null);
            Assert.NotNull(page["records"]);
        }
    }

    [RequiresQuickBooksFact]
    public async Task The_common_transactions_can_be_queried()
    {
        var runner = new QueryRunner(Client());

        foreach (var name in new[] { "Invoice", "Bill", "Check", "JournalEntry" })
        {
            var page = await runner.RunAsync(Entity.Find(name)!, new QueryFilters { TxnDateFrom = "2000-01-01" }, 5, null);
            Assert.NotNull(page["records"]);
        }
    }

    [RequiresQuickBooksFact]
    public async Task Core_reports_run()
    {
        var runner = new ReportRunner(Client());

        foreach (var report in new[] { "ARAgingSummary", "ProfitAndLossStandard", "BalanceSheetStandard", "TrialBalance" })
        {
            var table = await runner.RunAsync(report, new ReportOptions { DateMacro = report.EndsWith("Summary", StringComparison.Ordinal) ? "Today" : "ThisYearToDate" });
            Assert.NotEmpty(table["columns"]!.AsArray());
        }
    }
}
