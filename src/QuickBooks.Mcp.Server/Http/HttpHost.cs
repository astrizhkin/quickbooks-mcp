using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace QuickBooks.Mcp.Server.Http;

/// <summary>
/// The <c>--http</c> mode: the same server over MCP's Streamable HTTP, listening only on this computer's loopback
/// address, with every request needing the access key. Reaching it from elsewhere is up to a tunnel the user runs.
/// </summary>
public static class HttpHost
{
    /// <summary>Builds the web application; <paramref name="key"/> is what requests must present. <paramref name="configureServices"/> lets a test replace services.</summary>
    public static WebApplication Build(ServerConfig config, string key, string[] args, Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        ServerHost.Configure(builder, config);
        builder.WebHost.UseUrls($"http://{config.HttpBind}:{config.HttpPort}");
        builder.Services.AddQuickBooksMcp(config).WithHttpTransport();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (HttpAccess.IsAuthorized(context.Request.Headers.Authorization, context.Request.Headers["X-API-Key"], key))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
        });
        app.MapMcp("/mcp");
        return app;
    }

    /// <summary>Runs until stopped, telling the user the address and where the key is.</summary>
    public static async Task<int> RunAsync(ServerConfig config, string[] args)
    {
        var key = HttpAccess.ResolveKey(config.HttpKey, HttpAccess.KeyFilePath);
        var app = Build(config, key, args);
        Console.Error.WriteLine($"quickbooks-mcp is serving MCP at http://{config.HttpBind}:{config.HttpPort}/mcp"
            + (config.HttpBind is "127.0.0.1" or "localhost" ? " (this computer only)." : " (every network; the access key guards every request)."));
        Console.Error.WriteLine(config.HttpKey is null
            ? $"Access key: {key}  (saved in {HttpAccess.KeyFilePath})"
            : "Access key: the one in QB_HTTP_KEY.");
        Console.Error.WriteLine("Send it as \"Authorization: Bearer <key>\" or \"X-API-Key: <key>\". To reach this from another device, see docs/REMOTE-ACCESS.md.");
        await app.RunAsync();
        return 0;
    }
}
