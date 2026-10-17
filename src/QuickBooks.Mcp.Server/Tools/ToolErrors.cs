using System.Runtime.InteropServices;
using ModelContextProtocol;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Server.Tools;

/// <summary>Runs tool bodies and reports every failure in the same, actionable shape.</summary>
public static class ToolErrors
{
    /// <summary>Runs <paramref name="body"/>, converting known failures to an MCP tool error.</summary>
    public static async Task<string> RunAsync(Func<Task<string>> body)
    {
        try
        {
            return await body();
        }
        catch (QbException ex)
        {
            throw new McpException($"QuickBooks rejected the request (code {ex.StatusCode}): {ex.StatusMessage}");
        }
        catch (COMException ex)
        {
            throw new McpException($"Could not talk to QuickBooks: {ex.Message} Is QuickBooks installed, and has the app been authorized (--authorize)?");
        }
        catch (OperationCanceledException)
        {
            throw new McpException("QuickBooks did not answer in time. It may be busy or showing a dialog; try again.");
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
