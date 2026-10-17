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

// The stdio MCP host is added in issue #12.
Console.Error.WriteLine("quickbooks-mcp: use --authorize or --doctor");
return 1;
