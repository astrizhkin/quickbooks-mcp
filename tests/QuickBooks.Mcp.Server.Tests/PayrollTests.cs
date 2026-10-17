using System.Collections;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Tools;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class PayrollTests
{
    private static async Task<IList<McpClientTool>> ListTools(string[] args)
    {
        var config = ServerConfig.Load(args, new Hashtable());
        var toServer = new Pipe();
        var toClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IQbClient>(new FixtureQbClient());
        services.AddQuickBooksMcp(config).WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        await using var provider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _ = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);
        await using var client = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()), cancellationToken: cts.Token);
        var tools = await client.ListToolsAsync(cancellationToken: cts.Token);
        await cts.CancelAsync();
        return tools;
    }

    [Fact]
    public async Task Payroll_tools_exist_only_with_the_payroll_switch()
    {
        Assert.DoesNotContain(await ListTools([]), t => t.Name.Contains("payroll"));
        Assert.DoesNotContain(await ListTools(["--allow-writes"]), t => t.Name.Contains("payroll"));
        Assert.Contains(await ListTools(["--allow-payroll"]), t => t.Name == "get_payroll_report");
    }

    [Theory]
    [InlineData("PayrollItemWage")]
    [InlineData("PayrollItemNonWage")]
    public async Task Payroll_items_are_searched(string type)
    {
        var qb = new FixtureQbClient();

        var json = await new PayrollTools(qb).SearchPayrollItems(type);

        Assert.Equal(type + "QueryRq", qb.SentRequest());
        Assert.Contains("Example " + type, json);
    }

    [Theory]
    [InlineData("PayrollSummary", "PayrollSummaryReportQueryRq")]
    [InlineData("EmployeeEarningsSummary", "PayrollSummaryReportQueryRq")]
    [InlineData("PayrollTransactionDetail", "PayrollDetailReportQueryRq")]
    public async Task Payroll_reports_use_the_payroll_requests(string type, string request)
    {
        var qb = new FixtureQbClient();

        await new PayrollTools(qb).GetPayrollReport(type, "LastQuarter", employees: ["Pat"]);

        Assert.Equal(request, qb.SentRequest());
    }

    [Fact]
    public async Task Ordinary_reports_do_not_reach_payroll_and_payroll_reports_are_not_ordinary() =>
        await ToolTestHelpers.RejectsAsync(() => new ReportTools(new FixtureQbClient()).GetReport("PayrollSummary", "ThisYear"));

    [Fact]
    public async Task The_generic_query_refuses_payroll_items_unless_payroll_is_on()
    {
        var off = new QueryTools(new FixtureQbClient(), ServerConfig.Load([], new Hashtable()));
        var on = new QueryTools(new FixtureQbClient(), ServerConfig.Load(["--allow-payroll"], new Hashtable()));

        await ToolTestHelpers.RejectsAsync(() => off.QueryRecords("PayrollItemWage"));
        Assert.Contains("Example", await on.QueryRecords("PayrollItemWage"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Payroll_and_sensitive_redaction_are_independent(bool redactSensitive, bool redactPayroll)
    {
        var inner = new CannedClient("""<QBXML><QBXMLMsgsRs><EmployeeQueryRs statusCode="0"><EmployeeRet><SSN>123-45-6789</SSN><EmployeePayrollInfo><PayPeriod>Weekly</PayPeriod></EmployeePayrollInfo></EmployeeRet></EmployeeQueryRs></QBXMLMsgsRs></QBXML>""");

        var reply = await new RedactingQbClient(inner, redactSensitive, redactPayroll).SendAsync(v => v);

        Assert.Equal(redactSensitive, !reply.Contains("123-45-6789"));
        Assert.Equal(redactPayroll, !reply.Contains("Weekly"));
    }

    private sealed class CannedClient(string reply) : IQbClient
    {
        public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default) => Task.FromResult(reply);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Employee_payroll_details_are_masked_unless_payroll_is_allowed(bool maskPayroll, bool shown)
    {
        var client = new RedactingQbClient(new FixtureQbClient(), sensitive: true, payroll: maskPayroll);

        var reply = await client.SendAsync(v => QuickBooks.Mcp.Core.QbXml.QbXmlRequest.Build(v, "EmployeeQueryRq"));

        Assert.Equal(shown, reply.Contains("Weekly"));
    }
}
