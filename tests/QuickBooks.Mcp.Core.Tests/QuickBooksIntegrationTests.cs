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

public class QuickBooksIntegrationTests
{
    [RequiresQuickBooksFact]
    public async Task Host_query_returns_product_name()
    {
        var options = new QbOptions { CompanyFile = Environment.GetEnvironmentVariable("QB_COMPANY_FILE") };
        var client = new LockingQbClient(new ComQbClient(options), options);

        var response = await client.SendAsync("HostQueryRq");

        Assert.False(string.IsNullOrEmpty((string?)response.Records.First().Element("ProductName")));
    }
}
