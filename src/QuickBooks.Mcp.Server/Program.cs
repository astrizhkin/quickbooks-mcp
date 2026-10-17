using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server;
using QuickBooks.Mcp.Server.Cli;

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

if (args.Contains("--authorize")) return await SetupCommands.AuthorizeAsync(config.Qb, Console.Out);
if (args.Contains("--doctor")) return await SetupCommands.DoctorAsync(config.Qb, Console.Out);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Enum.Parse<LogLevel>(config.LogLevel));
// stdout carries the protocol, so all logs go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(config);
IQbClient qb = new LockingQbClient(new ComQbClient(config.Qb), config.Qb);
builder.Services.AddSingleton(config.Redact ? new RedactingQbClient(qb) : qb);
builder.Services.AddQuickBooksMcp(config).WithStdioServerTransport();

await builder.Build().RunAsync();
return 0;
