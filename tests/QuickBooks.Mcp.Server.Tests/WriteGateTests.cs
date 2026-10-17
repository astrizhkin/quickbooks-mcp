using System.Collections;
using System.ComponentModel;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Writes;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

[McpServerToolType]
public sealed class FakeWriteTools
{
    [McpServerTool(Name = "create_widget"), Description("Create.")]
    public string Create() => "created";

    [McpServerTool(Name = "update_widget"), Description("Update.")]
    public string Update() => "updated";

    [McpServerTool(Name = "delete_widget"), Description("Delete.")]
    public string Delete() => "deleted";

    [McpServerTool(Name = "void_widget"), Description("Void.")]
    public string Void() => "voided";
}

public class WriteGateTests
{
    private static async Task<IList<McpClientTool>> ListToolsAsync(string[] args)
    {
        var config = ServerConfig.Load(args, new Hashtable());
        var toServer = new Pipe();
        var toClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IQbClient>(new FixtureQbClient());
        services.AddQuickBooksMcp(config)
            .AddWriteTools(WriteGate.From(config), typeof(FakeWriteTools))
            .WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
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
    public async Task Default_config_registers_only_read_only_tools()
    {
        var tools = await ListToolsAsync([]);

        Assert.NotEmpty(tools);
        Assert.All(tools, t => Assert.True(t.ProtocolTool.Annotations?.ReadOnlyHint == true, $"{t.Name} is not read-only"));
        Assert.DoesNotContain(tools, t => WriteOps.FromToolName(t.Name) != WriteOp.None);
    }

    [Theory]
    [InlineData("--enable-write", new[] { "create_widget" })]
    [InlineData("--enable-update", new[] { "update_widget" })]
    [InlineData("--enable-delete", new[] { "delete_widget", "void_widget" })]
    [InlineData("--allow-writes", new[] { "create_widget", "update_widget", "delete_widget", "void_widget" })]
    public async Task Each_switch_registers_only_its_tools(string flag, string[] expected)
    {
        var names = (await ListToolsAsync([flag])).Select(t => t.Name).Where(n => n.EndsWith("_widget"));

        Assert.Equal(expected.Order(), names.Order());
    }

    [Fact]
    public void Gate_refuses_disabled_operations_and_entities()
    {
        var closed = new WriteGate(WriteOp.None);
        Assert.Throws<McpException>(() => closed.Require(WriteOp.Create, "Invoice"));

        var limited = new WriteGate(WriteOp.All, ["Invoice", "Customer"]);
        limited.Require(WriteOp.Update, "invoice");
        Assert.Throws<McpException>(() => limited.Require(WriteOp.Update, "Bill"));

        Assert.Throws<McpException>(() => new WriteGate(WriteOp.Create).Require(WriteOp.Delete, "Invoice"));
    }

    [Theory]
    [InlineData("create_invoice", WriteOp.Create)]
    [InlineData("update_customer", WriteOp.Update)]
    [InlineData("delete_bill", WriteOp.Delete)]
    [InlineData("void_check", WriteOp.Delete)]
    [InlineData("search_customers", WriteOp.None)]
    [InlineData("get_company", WriteOp.None)]
    public void Operation_comes_from_the_name_prefix(string name, WriteOp op) =>
        Assert.Equal(op, WriteOps.FromToolName(name));

    [Fact]
    public void Write_allow_list_is_parsed()
    {
        var c = ServerConfig.Load([], new Hashtable { ["QB_WRITE_ALLOW"] = "Invoice, Customer", ["QB_ENABLE_WRITE"] = "true" });

        Assert.Equal(WriteOp.Create, c.Writes);
        Assert.Equal(["Invoice", "Customer"], c.WriteAllow);
    }
}

public class WriteToolRegistrationTests
{
    [Fact]
    public async Task Enabling_create_registers_the_create_tools_and_the_field_helper()
    {
        var names = (await ListTools(["--enable-write"])).Select(t => t.Name).ToList();

        Assert.Contains("create_list_item", names);
        Assert.Contains("create_sales_transaction", names);
        Assert.Contains("create_purchase_transaction", names);
        Assert.Contains("describe_write_fields", names);
        Assert.DoesNotContain(names, n => n.StartsWith("update_", StringComparison.Ordinal) || n.StartsWith("delete_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Without_writes_neither_write_tools_nor_the_helper_exist()
    {
        var names = (await ListTools([])).Select(t => t.Name).ToList();

        Assert.DoesNotContain("create_list_item", names);
        Assert.DoesNotContain("describe_write_fields", names);
    }

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
}
