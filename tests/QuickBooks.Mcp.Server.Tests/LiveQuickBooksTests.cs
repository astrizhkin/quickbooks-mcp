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

    // Calls that this QuickBooks (Enterprise 24.0, sample product-based company) refuses. Each must keep failing with
    // exactly this code, so a call that starts working, or fails differently, is reported rather than ignored.
    // 3231: refused whatever the qbXML version (checked by hand against the raw reply): Leads, sales tax returns, payroll period.
    // 3250: the company file does not use the feature (multi-currency, advanced inventory with sites and bar codes).
    private static readonly (string Call, string Code)[] Refused =
    [
        ("query_records {\"entity\":\"Lead\"", "3231"),
        ("get_sales_tax_returns {}", "3231"),
        ("get_sales_tax_return_lines ", "3231"),
        ("get_payroll_last_period {}", "3231"),
        ("search_setup_lists {\"list\":\"Currency\"}", "3250"),
        ("query_records {\"entity\":\"Currency\"", "3250"),
        ("query_records {\"entity\":\"InventorySite\"", "3250"),
        ("query_records {\"entity\":\"TransferInventory\"", "3250"),
        ("search_inventory_transactions {\"type\":\"TransferInventory\"", "3250"),
        ("search_inventory_sites {}", "3250"),
        ("get_barcodes {}", "3250"),
        ("get_report {\"reportType\":\"InventoryValuationSummaryBySite\"", "3250"),
        ("get_report {\"reportType\":\"LotNumberInStockBySite\"", "3250"),
        ("get_report {\"reportType\":\"SerialNumberInStockBySite\"", "3250"),
    ];

    /// <summary>Null when the call did what was expected: worked, or failed with exactly the code listed in <see cref="Refused"/>.</summary>
    private static string? Judge(string tool, object args, bool ok, string text)
    {
        var call = $"{tool} {JsonSerializer.Serialize(args)}";
        var refused = Refused.FirstOrDefault(r => call.StartsWith(r.Call, StringComparison.Ordinal));
        if (refused.Call is null) return ok ? null : $"{call} -> {Short(text)}";
        return !ok && text.Contains($"(code {refused.Code})", StringComparison.Ordinal)
            ? null
            : $"{call} was expected to be refused with code {refused.Code} but gave: {Short(text)}";
    }

    // Not in the sweep because they need an argument or an opt-in; each has its own test below.
    private static readonly HashSet<string> Skipped =
    [
        "open_in_quickbooks", "describe_write_fields", "get_assemblies_can_build", "get_bills_to_pay", "get_event_subscriptions",
        "get_sales_tax_return_lines", "get_payroll_report", "search_payroll_items", "get_payroll_last_period",
    ];

    [LiveFact]
    public async Task Every_read_only_tool_works()
    {
        await using var session = await StartAsync();
        var tools = (await session.Client.ListToolsAsync()).Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true && !Skipped.Contains(t.Name)).ToList();
        var failures = new List<string>();
        var total = 0;

        // A few reports need a name to run for; use the first account and customer the file has.
        var account = FirstProperty((await CallAsync(session.Client, "search_accounts", new { maxResults = 1 })).Text, "FullName");
        var customer = FirstProperty((await CallAsync(session.Client, "search_customers", new { maxResults = 1 })).Text, "FullName");
        object ReportCall(string type, object fallback) => type switch
        {
            "PhysicalInventoryWorksheet" => new { reportType = type },
            "MissingChecks" => new { reportType = type, dateMacro = "ThisYearToDate", accounts = new[] { account } },
            "JobEstimatesVsActualsDetail" or "JobProfitabilityDetail" => new { reportType = type, dateMacro = "ThisYearToDate", names = new[] { customer } },
            "CustomSummary" => new { reportType = type, dateMacro = "ThisYearToDate", summarizeRowsBy = "Account", summarizeColumnsBy = "TotalOnly" },
            "CustomTxnDetail" => new { reportType = type, dateMacro = "ThisYearToDate", summarizeRowsBy = "Account", includeColumns = new[] { "Amount" } },
            _ => fallback,
        };

        foreach (var tool in tools)
        {
            var calls = Calls.GetValueOrDefault(tool.Name) ?? [new { }];
            if (tool.Name == "get_report")
                calls = [.. calls.Select(c => ReportCall(JsonSerializer.SerializeToElement(c).GetProperty("reportType").GetString()!, c))];
            foreach (var args in calls)
            {
                total++;
                var (ok, text) = await CallAsync(session.Client, tool.Name, args);
                if (Judge(tool.Name, args, ok, text) is { } failure) failures.Add(failure);
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
    public async Task Bills_to_pay_works_for_a_vendor()
    {
        await using var session = await StartAsync();
        var (ok, text) = await CallAsync(session.Client, "search_vendors", new { maxResults = 1 });
        Assert.True(ok, text);
        var name = FirstFullName(text);
        if (name is null) return; // the file has no vendors

        var (paid, result) = await CallAsync(session.Client, "get_bills_to_pay", new { vendor = name });
        Assert.True(paid, Short(result));
    }

    [LiveFact]
    public async Task Writes_are_off_by_default()
    {
        await using var session = await StartAsync();
        var names = (await session.Client.ListToolsAsync()).Select(t => t.Name).ToList();

        Assert.DoesNotContain(names, n => n.StartsWith("create_", StringComparison.Ordinal) || n.StartsWith("update_", StringComparison.Ordinal) ||
                                          n.StartsWith("delete_", StringComparison.Ordinal) || n.StartsWith("void_", StringComparison.Ordinal) || n.StartsWith("merge_", StringComparison.Ordinal));
        Assert.DoesNotContain("get_payroll_report", names);
    }

    [LiveFact]
    public async Task Sales_tax_return_lines_work_for_a_tax_agency()
    {
        await using var session = await StartAsync();
        var (ok, text) = await CallAsync(session.Client, "search_items", new { itemType = "SalesTax", maxResults = 5 });
        Assert.True(ok, text);
        using var doc = JsonDocument.Parse(text);
        var agency = doc.RootElement.GetProperty("records").EnumerateArray()
            .Select(r => r.TryGetProperty("TaxVendorRef", out var v) && v.TryGetProperty("FullName", out var n) ? n.GetString() : null)
            .FirstOrDefault(n => n is not null);
        Assert.True(agency is not null, "the company file has no sales tax item with a tax agency to test with");

        var args = new { taxAgencies = new[] { agency } };
        var (done, result) = await CallAsync(session.Client, "get_sales_tax_return_lines", args);
        Assert.Null(Judge("get_sales_tax_return_lines", args, done, result));
    }

    [LiveFact]
    public async Task Event_subscriptions_can_be_queried_for_every_kind()
    {
        await using var session = await StartAsync();
        var failures = new List<string>();
        foreach (var kind in new[] { "DataEventSubscription", "UIEventSubscription", "UIExtensionSubscription", "DataEventRecoveryInfo" })
        {
            var (ok, text) = await CallAsync(session.Client, "get_event_subscriptions", new { kind, subscriberId = Guid.NewGuid().ToString() });
            if (!ok) failures.Add($"{kind} -> {Short(text)}");
        }

        AssertNoFailures(failures, 4);
    }

    [LiveFact]
    public async Task Payroll_tools_work_when_payroll_is_allowed()
    {
        await using var session = await StartAsync("--allow-payroll");
        var failures = new List<string>();
        var calls = new List<(string Tool, object Args)>
        {
            ("get_payroll_last_period", new { }),
            ("search_payroll_items", new { type = "PayrollItemWage", maxResults = 5 }),
            ("search_payroll_items", new { type = "PayrollItemNonWage", maxResults = 5 }),
        };
        calls.AddRange(ReportKind.Payroll.SelectMany(k => k.Types).Select(t => ("get_payroll_report", (object)new { reportType = t, dateMacro = "ThisYearToDate" })));
        foreach (var (tool, args) in calls)
        {
            var (ok, text) = await CallAsync(session.Client, tool, args);
            if (Judge(tool, args, ok, text) is { } failure) failures.Add(failure);
        }

        AssertNoFailures(failures, calls.Count);
    }

    [LiveFact]
    public async Task Write_fields_are_described_for_the_common_entities()
    {
        // The tool only exists when writes are on; describing fields changes nothing.
        await using var session = await StartAsync("--allow-writes");
        var failures = new List<string>();
        foreach (var entity in new[] { "Customer", "Vendor", "Invoice", "Bill", "JournalEntry", "ItemService" })
            foreach (var operation in new[] { "create", "update" })
            {
                var (ok, text) = await CallAsync(session.Client, "describe_write_fields", new { entity, operation });
                if (!ok || text.Length == 0) failures.Add($"{entity} {operation} -> {Short(text)}");
            }

        AssertNoFailures(failures, 12);
    }

    [LiveFact]
    public async Task A_record_can_be_opened_in_quickbooks()
    {
        await using var session = await StartAsync();
        var found = await CallAsync(session.Client, "search_customers", new { maxResults = 1 });
        var id = FirstProperty(found.Text, "ListID");
        Assert.False(id is null, $"no customer to open: {Short(found.Text)}");

        var (ok, text) = await CallAsync(session.Client, "open_in_quickbooks", new { recordType = "Customer", id });
        Assert.True(ok, Short(text));
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

        // QuickBooks Enterprise 24.0 only makes a deleted customer inactive, so look at inactive ones too:
        // the customer must be gone or inactive, never still active.
        var all = await CallAsync(client, "search_customers", new { nameContains = name, active = "all", maxResults = 1 });
        Assert.True(all.Ok, all.Text);
        Assert.True(FirstProperty(all.Text, "IsActive") is null or "false", $"customer is still active after delete: {Short(all.Text)}");
    }

    /// <summary>Calls a destructive tool the way a client that cannot ask the user does: preview first, then again with the token.</summary>
    private static async Task<(bool Ok, string Text)> ConfirmedAsync(McpClient client, string tool, Dictionary<string, object?> args)
    {
        var (ok, text) = await CallAsync(client, tool, args);
        if (!ok) return (ok, text);
        args["confirmationToken"] = JsonDocument.Parse(text).RootElement.GetProperty("confirmation_token").GetString();
        return await CallAsync(client, tool, args);
    }

    [LiveWriteFact]
    public async Task One_customer_can_be_merged_into_another()
    {
        // QuickBooks refuses list merges (code 3500) unless the company file is open in single-user mode.
        await using var session = await StartAsync("--allow-writes");
        var client = session.Client;
        var stamp = Guid.NewGuid().ToString("N")[..12];
        string[] names = [$"MCP merge from {stamp}", $"MCP merge to {stamp}"];
        var ids = new List<string>();
        try
        {
            foreach (var name in names)
            {
                var (created, text) = await CallAsync(client, "create_list_item", new { entity = "Customer", data = new { Name = name }, idempotencyKey = name });
                Assert.True(created, text);
                var id = FirstProperty((await CallAsync(client, "search_customers", new { nameContains = name, maxResults = 1 })).Text, "ListID");
                Assert.False(id is null, $"created customer '{name}' not found");
                ids.Add(id!);
            }

            var (merged, result) = await ConfirmedAsync(client, "merge_records", new() { ["entity"] = "Customer", ["fromId"] = ids[0], ["toId"] = ids[1] });
            Assert.True(merged, result);
            Assert.Null(FirstFullName((await CallAsync(client, "search_customers", new { nameContains = names[0] })).Text));
            Assert.NotNull(FirstFullName((await CallAsync(client, "search_customers", new { nameContains = names[1] })).Text));
        }
        finally
        {
            // Leave nothing behind, whichever step failed: delete whatever of the two still exists.
            foreach (var name in names)
            {
                var id = FirstProperty((await CallAsync(client, "search_customers", new { nameContains = name, maxResults = 1 })).Text, "ListID");
                if (id is not null) await ConfirmedAsync(client, "delete_record", new() { ["entity"] = "Customer", ["id"] = id });
            }
        }

        foreach (var name in names)
            Assert.Null(FirstFullName((await CallAsync(client, "search_customers", new { nameContains = name })).Text));
    }

    [LiveWriteFact]
    public async Task Deleting_the_subscriptions_of_an_unknown_subscriber_is_refused_as_not_found_for_every_kind()
    {
        // No subscription can be created through this server, so the delete can only be exercised on a subscriber
        // that does not exist; QuickBooks must answer 3120 (not found) for each kind, which also proves the request is accepted.
        await using var session = await StartAsync("--allow-writes");
        var failures = new List<string>();
        foreach (var kind in new[] { "Data", "UI", "UIExtension", "DataEventRecoveryInfo" })
        {
            var (ok, text) = await ConfirmedAsync(session.Client, "delete_event_subscription", new() { ["kind"] = kind, ["subscriberId"] = Guid.NewGuid().ToString() });
            if (ok || !text.Contains("(code 3120)", StringComparison.Ordinal)) failures.Add($"{kind} -> expected code 3120 but gave: {Short(text)}");
        }

        AssertNoFailures(failures, 4);
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
