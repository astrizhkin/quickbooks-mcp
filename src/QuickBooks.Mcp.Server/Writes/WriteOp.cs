namespace QuickBooks.Mcp.Server.Writes;

/// <summary>The kinds of change a tool can make. Combine to say which are enabled.</summary>
[Flags]
public enum WriteOp
{
    /// <summary>Nothing may change (the default).</summary>
    None = 0,

    /// <summary>create_* tools.</summary>
    Create = 1,

    /// <summary>update_* tools.</summary>
    Update = 2,

    /// <summary>delete_*, void_* and merge_* tools.</summary>
    Delete = 4,

    /// <summary>Everything.</summary>
    All = Create | Update | Delete,
}

/// <summary>Maps tool names to operations; the one place that knows the naming convention.</summary>
public static class WriteOps
{
    /// <summary>The operation a tool performs, or <see cref="WriteOp.None"/> for read tools.</summary>
    public static WriteOp FromToolName(string toolName) => toolName.Split('_')[0] switch
    {
        "create" => WriteOp.Create,
        "update" => WriteOp.Update,
        "delete" or "void" or "merge" => WriteOp.Delete,
        _ => WriteOp.None,
    };
}
