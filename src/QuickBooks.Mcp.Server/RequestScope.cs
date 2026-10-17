using ModelContextProtocol.Server;

namespace QuickBooks.Mcp.Server;

/// <summary>The MCP server that is handling the current tool call, so deep code can ask the connected client a question.</summary>
public static class RequestScope
{
    private static readonly AsyncLocal<McpServer?> Current = new();

    /// <summary>The server for the current call, or null outside a call.</summary>
    public static McpServer? Server => Current.Value;

    /// <summary>Marks <paramref name="server"/> as current until the returned object is disposed.</summary>
    public static IDisposable Enter(McpServer server)
    {
        var previous = Current.Value;
        Current.Value = server;
        return new Scope(previous);
    }

    private sealed class Scope(McpServer? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
