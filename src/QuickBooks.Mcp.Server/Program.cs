using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickBooks.Mcp.Server;
using QuickBooks.Mcp.Server.Cli;
using QuickBooks.Mcp.Server.Http;

ServerConfig config;
try
{
    config = ServerConfig.Load(args, Environment.GetEnvironmentVariables());
}
catch (ConfigException ex)
{
    Console.Error.WriteLine($"quickbooks-mcp: {ex.Message}");
    return 2;
}

if (args.Contains("--tools-reference"))
{
    Console.Out.Write(ToolReference.Generate());
    return 0;
}

if (args.Contains("--authorize")) return await SetupCommands.AuthorizeAsync(config.Qb, Console.Out);
if (args.Contains("--diagnostics")) return await DiagnosticsCommand.RunAsync(config, Console.Out);
if (args.Contains("--doctor")) return await SetupCommands.DoctorAsync(config.Qb, Console.Out);

if (config.Http) return await HttpHost.RunAsync(config, args);

var builder = Host.CreateApplicationBuilder(args);
ServerHost.Configure(builder, config);
builder.Services.AddQuickBooksMcp(config).WithStdioServerTransport();

await builder.Build().RunAsync();
return 0;
