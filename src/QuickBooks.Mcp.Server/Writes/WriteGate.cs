using ModelContextProtocol;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>
/// Decides what may change. Write tools are only registered for enabled operations, and each
/// calls <see cref="Require"/> before sending anything, so a disabled entity is refused even if registered.
/// </summary>
public sealed class WriteGate(WriteOp enabled, IReadOnlyCollection<string>? allowedEntities = null)
{
    /// <summary>Builds the gate from the server configuration.</summary>
    public static WriteGate From(ServerConfig config) => new(config.Writes, config.WriteAllow);

    /// <summary>True when the operation is switched on.</summary>
    public bool Allows(WriteOp op) => op != WriteOp.None && (enabled & op) == op;

    /// <summary>Throws a tool error unless <paramref name="op"/> is enabled for <paramref name="entity"/> (e.g. "Invoice").</summary>
    public void Require(WriteOp op, string entity)
    {
        if (!Allows(op))
            throw new McpException($"{op} is disabled. Start the server with --allow-writes (or QB_ENABLE_{EnvName(op)}=true) to enable it.");
        if (allowedEntities is not null && !allowedEntities.Contains(entity, StringComparer.OrdinalIgnoreCase))
            throw new McpException($"Writing {entity} is not allowed. Allowed: {string.Join(", ", allowedEntities)} (QB_WRITE_ALLOW).");
    }

    private static string EnvName(WriteOp op) => op switch
    {
        WriteOp.Create => "WRITE",
        WriteOp.Update => "UPDATE",
        _ => "DELETE",
    };
}
