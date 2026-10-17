using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>The answer to "may I make this change?".</summary>
public enum Confirmation
{
    /// <summary>The user said yes.</summary>
    Approved,

    /// <summary>The user said no or dismissed the question.</summary>
    Declined,

    /// <summary>The client cannot ask the user, so the preview-and-token flow is used instead.</summary>
    Unavailable,
}

/// <summary>Asks the person using the AI app to approve a change.</summary>
public interface IUserConfirmation
{
    /// <summary>Shows <paramref name="message"/> to the user and returns what they decided.</summary>
    Task<Confirmation> AskAsync(string message, CancellationToken cancellationToken);
}

/// <summary>Asks through MCP elicitation, when the connected client supports it.</summary>
public sealed class ElicitationConfirmation(Func<McpServer?> server) : IUserConfirmation
{
    /// <inheritdoc />
    public async Task<Confirmation> AskAsync(string message, CancellationToken cancellationToken)
    {
        var current = server();
        if (current?.ClientCapabilities?.Elicitation is null) return Confirmation.Unavailable;

        var result = await current.ElicitAsync(new ElicitRequestParams { Message = message, RequestedSchema = new ElicitRequestParams.RequestSchema() }, cancellationToken);
        return result.Action == "accept" ? Confirmation.Approved : Confirmation.Declined;
    }
}
