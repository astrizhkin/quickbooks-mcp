using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server;
using QuickBooks.Mcp.Server.Cli;
using QuickBooks.Mcp.Server.Diagnostics;

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

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Enum.Parse<LogLevel>(config.LogLevel));
// stdout carries the protocol, so all logs go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
if (config.LogFile is not null) builder.Logging.AddProvider(new FileLoggerProvider(config.LogFile));
builder.Services.AddSingleton(config);
var locking = new LockingQbClient(new ComQbClient(config.Qb), config.Qb);
builder.Services.AddSingleton<IQbClient>(sp =>
{
    IQbClient qb = new LoggingQbClient(locking, sp.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingQbClient>());
    return config.Redact || !config.AllowPayroll ? new RedactingQbClient(qb, sensitive: config.Redact, payroll: !config.AllowPayroll) : qb;
});
builder.Services.AddQuickBooksMcp(config).WithStdioServerTransport();

await builder.Build().RunAsync();
return 0;
