using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class McpProtocolTests
{
    [Fact]
    public async Task Client_lists_tools_and_calls_get_company()
    {
        var toServer = new Pipe();
        var toClient = new Pipe();
        var fixtures = new FixtureQbClient();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IQbClient>(fixtures);
        services.AddQuickBooksMcp(ServerConfig.Load([], new System.Collections.Hashtable()))
            .WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        await using var provider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var serverTask = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);

        await using var client = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()),
            cancellationToken: cts.Token);

        Assert.Contains(await client.ListToolsAsync(cancellationToken: cts.Token), t => t.Name == "get_company");
        var result = await client.CallToolAsync("get_company", cancellationToken: cts.Token);

        Assert.True(result.IsError != true);
        Assert.Contains("Sample Company", result.Content.OfType<TextContentBlock>().Single().Text);
        Assert.Contains("CompanyQueryRq", fixtures.Requests.Single().ToString());

        await cts.CancelAsync();
        try { await serverTask; } catch (OperationCanceledException) { }
    }
}

public class VersionTests
{
    [Fact]
    public void Server_version_comes_from_the_build() => Assert.Matches(@"^\d+\.\d+\.\d+", McpSetup.Version);
}
