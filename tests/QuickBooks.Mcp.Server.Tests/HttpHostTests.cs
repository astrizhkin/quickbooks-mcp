using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Http;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Server.Tests;

public class HttpHostTests
{
    private const string Key = "test-key-0123456789abcdef";

    private static async Task<(WebApplication App, string Url)> StartAsync()
    {
        var config = new ServerConfig { Qb = new QbOptions(), Http = true, HttpPort = 0 };
        var app = HttpHost.Build(config, Key, [], services => services.AddSingleton<IQbClient>(new FixtureQbClient()));
        await app.StartAsync();
        return (app, app.Urls.First());
    }

    [Fact]
    public async Task Requests_without_the_key_are_refused()
    {
        var (app, url) = await StartAsync();
        await using var _ = app;
        using var http = new HttpClient();

        var none = await http.PostAsync(url + "/mcp", new StringContent("{}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-wrong-wrong-wrong");
        var wrong = await http.PostAsync(url + "/mcp", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task A_client_with_the_key_lists_tools_and_calls_one()
    {
        var (app, url) = await StartAsync();
        await using var _ = app;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(url + "/mcp"),
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + Key },
        });

        await using var client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);

        Assert.Contains(await client.ListToolsAsync(cancellationToken: cts.Token), t => t.Name == "get_company");
        var result = await client.CallToolAsync("get_company", cancellationToken: cts.Token);
        Assert.True(result.IsError != true);
        Assert.Contains("Sample Company", result.Content.OfType<TextContentBlock>().Single().Text);
    }
}
