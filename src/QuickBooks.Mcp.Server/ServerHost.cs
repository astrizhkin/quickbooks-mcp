using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Diagnostics;

namespace QuickBooks.Mcp.Server;

/// <summary>The logging and QuickBooks client wiring shared by the stdio and HTTP modes.</summary>
public static class ServerHost
{
    /// <summary>Configures logging (to stderr, because stdout may carry the protocol) and the decorated QuickBooks client.</summary>
    public static void Configure(IHostApplicationBuilder builder, ServerConfig config)
    {
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Enum.Parse<LogLevel>(config.LogLevel));
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        if (config.LogFile is not null) builder.Logging.AddProvider(new FileLoggerProvider(config.LogFile));
        builder.Services.AddSingleton(config);
        var locking = new LockingQbClient(new ComQbClient(config.Qb), config.Qb);
        builder.Services.AddSingleton<IQbClient>(sp =>
        {
            IQbClient qb = new LoggingQbClient(locking, sp.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingQbClient>());
            qb = new CachingQbClient(qb, config.CacheLifetime);
            return config.Redact || !config.AllowPayroll ? new RedactingQbClient(qb, sensitive: config.Redact, payroll: !config.AllowPayroll) : qb;
        });
    }
}
