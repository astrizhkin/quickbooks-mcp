using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Tests;

/// <summary>Runs only when <c>QB_COMPANY_FILE</c> is set (use Intuit's sample company file or a copy of it).</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QB_COMPANY_FILE")))
            Skip = "Set QB_COMPANY_FILE to run the live QuickBooks tests.";
    }
}

/// <summary>Runs only when <c>QB_TEST_WRITES=1</c> as well: these tests create and delete a record in the company file.</summary>
public sealed class LiveWriteFactAttribute : FactAttribute
{
    public LiveWriteFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QB_COMPANY_FILE")) ||
            Environment.GetEnvironmentVariable("QB_TEST_WRITES") != "1")
            Skip = "Set QB_COMPANY_FILE and QB_TEST_WRITES=1 (on a copy of the sample company) to run the live write tests.";
    }
}

/// <summary>
/// The whole tool surface against a real QuickBooks, through the MCP protocol exactly as an AI app uses it.
/// Each test collects every failure and reports them together, so one run shows everything that is wrong.
/// </summary>
public sealed class LiveQuickBooksTests
{
    private sealed class Session(McpClient client, Func<Task> stop) : IAsyncDisposable
    {
        public McpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await stop();
        }
    }

    /// <summary>Starts the real server (COM client, lock, cache, redaction) in this process and connects a client to it.</summary>
    private static async Task<Session> StartAsync(params string[] args)
    {
        var config = ServerConfig.Load(args, Environment.GetEnvironmentVariables());
        var toServer = new Pipe();
        var toClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IQbClient>(_ =>
        {
            IQbClient qb = new CachingQbClient(new LockingQbClient(new ComQbClient(config.Qb), config.Qb), config.CacheLifetime);
            return config.Redact || !config.AllowPayroll ? new RedactingQbClient(qb, sensitive: config.Redact, payroll: !config.AllowPayroll) : qb;
        });
        services.AddQuickBooksMcp(config).WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        var provider = services.BuildServiceProvider();
        var cts = new CancellationTokenSource();
        var serverTask = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);
        var client = await McpClient.CreateAsync(new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()));
        return new Session(client, async () =>
        {
            await cts.CancelAsync();
            try { await serverTask; } catch (OperationCanceledException) { }
            await provider.DisposeAsync();
        });
    }

    private static async Task<(bool Ok, string Text)> CallAsync(McpClient client, string tool, object? args = null)
    {
        try
        {
            var dict = args is null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(args));
            var result = await client.CallToolAsync(tool, dict, cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token);
            return (result.IsError != true, string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string Short(string text) => text.Length <= 300 ? text : text[..300] + "...";

    private static void AssertNoFailures(List<string> failures, int total)
    {
        Assert.True(failures.Count == 0, $"{failures.Count} of {total} failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    // Payroll lists are behind their own opt-in, so a default server rightly refuses them.
    private static bool IsPayroll(Entity e) => e.Name.StartsWith("PayrollItem", StringComparison.Ordinal);

    // Tools that need arguments, with the calls that cover them. Everything else is called with no arguments.
    private static readonly Dictionary<string, object[]> Calls = new()
    {
        ["search_activities"] = [new { type = "TimeTracking" }, new { type = "VehicleMileage" }, new { type = "ToDo" }],
        ["search_inventory_transactions"] = [new { type = "InventoryAdjustment" }, new { type = "TransferInventory" }, new { type = "BuildAssembly" }],
        ["get_changes_since"] = [new { since = "2000-01-01", entities = new[] { "Customer", "Vendor", "Item", "Invoice", "Bill" } }],
        ["get_deleted_records"] = [new { kind = "list", since = "2000-01-01" }, new { kind = "transaction", since = "2000-01-01" }],
        ["search_setup_lists"] = [.. new[] { "Class", "Currency", "StandardTerms", "DateDrivenTerms", "PaymentMethod", "ShipMethod", "CustomerMsg", "SalesTaxCode", "SalesRep", "Template", "UnitOfMeasureSet", "Vehicle", "BillingRate", "PriceLevel", "WorkersCompCode", "CustomerType", "VendorType", "JobType" }.Select(l => (object)new { list = l })],
        ["query_records"] = [.. Entity.All.Where(e => !IsPayroll(e)).Select(e => (object)new { entity = e.Name, maxResults = 5 })],
        ["get_report"] = [.. ReportKind.All.SelectMany(k => k.Types.Select(t => k.Request == "BudgetSummaryReportQueryRq"
            ? (object)new { reportType = t, fiscalYear = DateTime.Today.Year }
            : new { reportType = t, dateMacro = "ThisYearToDate" }))],
        ["get_aged_receivables"] = [new { }, new { detail = true }],
        ["get_aged_payables"] = [new { }, new { detail = true }],
        ["get_profit_and_loss"] = [new { dateMacro = "ThisYearToDate" }, new { dateMacro = "ThisYearToDate", summarizeColumnsBy = "Month" }],
        ["get_balance_sheet"] = [new { dateMacro = "ThisYearToDate" }],
        ["get_trial_balance"] = [new { dateMacro = "ThisYearToDate" }],
        ["get_general_ledger"] = [new { dateMacro = "ThisMonth" }],
        ["search_customers"] = [new { maxResults = 5 }, new { maxResults = 5, active = "all" }],
        ["search_vendors"] = [new { maxResults = 5 }],
        ["search_employees"] = [new { maxResults = 5 }],
        ["search_items"] = [new { maxResults = 5 }, new { maxResults = 5, itemType = "Service" }, new { maxResults = 5, itemType = "Inventory" }],
        ["search_names"] = [new { maxResults = 5 }],
        ["search_sales_transactions"] = [.. new[] { "Invoice", "SalesReceipt", "CreditMemo", "Estimate", "SalesOrder", "ReceivePayment", "ARRefundCreditCard", "Charge", "Deposit" }.Select(t => (object)new { type = t, maxResults = 5 })],
        ["search_purchase_transactions"] = [.. new[] { "Bill", "BillPaymentCheck", "BillPaymentCreditCard", "PurchaseOrder", "ItemReceipt", "VendorCredit", "Check", "CreditCardCharge", "CreditCardCredit", "SalesTaxPaymentCheck", "Transfer", "JournalEntry" }.Select(t => (object)new { type = t, maxResults = 5 })],
        ["search_transactions"] = [new { maxResults = 5, dateFrom = "2000-01-01" }],
    };

    // Not called: open_in_quickbooks changes what is on screen, payroll needs its own opt-in,
    // get_assemblies_can_build and describe_write_fields are covered below.
    private static readonly HashSet<string> Skipped = ["open_in_quickbooks", "get_payroll_report", "search_payroll_items", "get_assemblies_can_build", "describe_write_fields"];

    [LiveFact]
    public async Task Every_read_only_tool_works()
    {
        await using var session = await StartAsync();
        var tools = (await session.Client.ListToolsAsync()).Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true && !Skipped.Contains(t.Name)).ToList();
        var failures = new List<string>();
        var total = 0;

        foreach (var tool in tools)
        {
            foreach (var args in Calls.GetValueOrDefault(tool.Name) ?? [new { }])
            {
                total++;
                var (ok, text) = await CallAsync(session.Client, tool.Name, args);
                // 3250: the company file does not use that feature (multi-currency, multiple inventory sites), which is not a defect.
                if (!ok && !text.Contains("(code 3250)", StringComparison.Ordinal)) failures.Add($"{tool.Name} {JsonSerializer.Serialize(args)} -> {Short(text)}");
            }
        }

        Assert.True(total > 0);
        AssertNoFailures(failures, total);
    }

    [LiveFact]
    public async Task Every_entity_and_report_in_the_catalog_is_covered_by_a_tool_call()
    {
        // A new entity or report type is queried automatically; this guards the call table above against going stale.
        Assert.Equal(Entity.All.Count(e => !IsPayroll(e)), Calls["query_records"].Length);
        Assert.Equal(ReportKind.All.Sum(k => k.Types.Count), Calls["get_report"].Length);
        await using var session = await StartAsync();
        var read = (await session.Client.ListToolsAsync()).Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true).Select(t => t.Name);
        Assert.Empty(Calls.Keys.Except(read));
    }

    [LiveFact]
    public async Task Assembly_build_capacity_works_when_the_file_has_an_assembly()
    {
        await using var session = await StartAsync();
        var (ok, text) = await CallAsync(session.Client, "search_items", new { itemType = "InventoryAssembly", maxResults = 1 });
        Assert.True(ok, text);
        var name = FirstFullName(text);
        if (name is null) return; // the file has no assemblies; nothing to build

        var (built, result) = await CallAsync(session.Client, "get_assemblies_can_build", new { assembly = name });
        Assert.True(built, Short(result));
    }

    [LiveFact]
    public async Task Writes_are_off_by_default()
    {
        await using var session = await StartAsync();
        var names = (await session.Client.ListToolsAsync()).Select(t => t.Name).ToList();

        Assert.DoesNotContain(names, n => n.StartsWith("create_", StringComparison.Ordinal) || n.StartsWith("update_", StringComparison.Ordinal) ||
                                          n.StartsWith("delete_", StringComparison.Ordinal) || n.StartsWith("void_", StringComparison.Ordinal));
        Assert.DoesNotContain("get_payroll_report", names);
    }

    [LiveWriteFact]
    public async Task A_customer_can_be_created_changed_and_deleted_with_confirmation()
    {
        await using var session = await StartAsync("--allow-writes");
        var client = session.Client;
        var name = $"MCP live test {Guid.NewGuid():N}"[..30];

        var (ok, text) = await CallAsync(client, "create_list_item", new { entity = "Customer", data = new { Name = name }, dryRun = true });
        Assert.True(ok, text);
        Assert.Null(FirstFullName((await CallAsync(client, "search_customers", new { nameContains = name })).Text));

        (ok, text) = await CallAsync(client, "create_list_item", new { entity = "Customer", data = new { Name = name }, idempotencyKey = name });
        Assert.True(ok, text);

        var found = await CallAsync(client, "search_customers", new { nameContains = name, maxResults = 1 });
        var id = FirstProperty(found.Text, "ListID");
        Assert.False(id is null, $"created customer not found: {Short(found.Text)}");

        (ok, text) = await CallAsync(client, "update_list_item", new { entity = "Customer", id, data = new { Notes = "changed by the live test" } });
        Assert.True(ok, text);

        (ok, text) = await CallAsync(client, "delete_record", new { entity = "Customer", id });
        Assert.True(ok, text);
        var token = JsonDocument.Parse(text).RootElement.GetProperty("confirmation_token").GetString();

        (ok, text) = await CallAsync(client, "delete_record", new { entity = "Customer", id, confirmationToken = token });
        Assert.True(ok, text);
        Assert.Null(FirstFullName((await CallAsync(client, "search_customers", new { nameContains = name })).Text));
    }

    private static string? FirstFullName(string json) => FirstProperty(json, "FullName") ?? FirstProperty(json, "Name");

    /// <summary>The first <paramref name="property"/> value inside the first record of a search result, or null when there are no records.</summary>
    private static string? FirstProperty(string json, string property)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array || records.GetArrayLength() == 0)
            return null;
        return records[0].TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
