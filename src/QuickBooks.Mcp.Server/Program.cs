using QuickBooks.Mcp.Core.Session;
using QuickBooks.Mcp.Server.Cli;

// shortcut: hand-rolled args, replaced by the configuration model in issue #10.
var options = new QbOptions { CompanyFile = Environment.GetEnvironmentVariable("QB_COMPANY_FILE") };

if (args.Contains("--authorize")) return await SetupCommands.AuthorizeAsync(options, Console.Out);
if (args.Contains("--doctor")) return await SetupCommands.DoctorAsync(options, Console.Out);

// The stdio MCP host is added in issue #12.
Console.Error.WriteLine("quickbooks-mcp: use --authorize or --doctor");
return 1;
