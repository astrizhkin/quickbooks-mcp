using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using QuickBooks.Mcp.Server.Prompts;
using QuickBooks.Mcp.Server.Resources;
using QuickBooks.Mcp.Server.Tools;

namespace QuickBooks.Mcp.Server;

/// <summary>Registers the MCP server and its tools; the transport and <c>IQbClient</c> are the caller's choice.</summary>
public static class McpSetup
{
    private const string Instructions = """
        Tools read data from the QuickBooks Desktop company file open on the user's computer.
        Prefer search_* tools with filters over broad queries, and ask the user before assuming a customer, vendor or date range.
        Amounts are in the company's home currency unless a tool says otherwise.
        """;

    /// <summary>Adds the MCP server with all tools allowed by the configuration.</summary>
    public static IMcpServerBuilder AddQuickBooksMcp(this IServiceCollection services, ServerConfig config)
    {
        var builder = services.AddMcpServer(o =>
        {
            o.ServerInfo = new() { Name = "quickbooks-mcp", Version = "0.1.0" };
            o.ServerInstructions = Instructions;
        }).WithTools<CompanyTools>().WithTools<QueryTools>().WithTools<SetupTools>().WithTools<NameTools>().WithTools<ItemTools>().WithTools<SalesTools>().WithTools<PurchaseTools>().WithTools<ReportTools>().WithResources<ListResources>().WithPrompts<WorkflowPrompts>();

        // Write tools are registered here only when config.AllowWrites is set (issue #25).
        return builder;
    }
}
