# @devwithmj/quickbooks-mcp

MCP server for **QuickBooks Desktop** (Windows). This package is a small launcher: on first run it downloads the matching release
build from GitHub, checks it against the SHA-256 shipped inside this package, caches it, and starts it with your MCP client's stdio.

```json
{
  "mcpServers": {
    "quickbooks": {
      "command": "npx",
      "args": ["-y", "@devwithmj/quickbooks-mcp"],
      "env": { "QB_COMPANY_FILE": "C:\\Path\\To\\Company.qbw" }
    }
  }
}
```

Run `npx @devwithmj/quickbooks-mcp --authorize` once, as a QuickBooks admin, to allow access, and `--doctor` to diagnose problems.
See the [project README](https://github.com/devwithmj/quickbooks-mcp) for everything else.

## Turn on changes

The server is read-only until you opt in. Add `--allow-writes` after the package name:

```json
"args": ["-y", "@devwithmj/quickbooks-mcp", "--allow-writes"]
```

or set `QB_ALLOW_WRITES=true` in `env`. To allow only one kind, use `QB_ENABLE_WRITE` (create), `QB_ENABLE_UPDATE` or `QB_ENABLE_DELETE` (delete, void, merge), and `QB_WRITE_ALLOW=Invoice,Customer` to limit which record types can change. Deletes and voids always ask for a confirmation step. See [the writes guide](https://github.com/devwithmj/quickbooks-mcp/blob/main/docs/WRITES.md).

## Use it over HTTP (advanced)

For a client that cannot start a local program, run `npx -y @devwithmj/quickbooks-mcp --http`. It serves MCP on `http://127.0.0.1:8765/mcp` for this computer only, and every request needs the access key it prints (or `QB_HTTP_KEY`). Changes stay off unless you also pass `--http-allow-writes`. To reach it from another device, add your own tunnel: [REMOTE-ACCESS.md](https://github.com/devwithmj/quickbooks-mcp/blob/main/docs/REMOTE-ACCESS.md).

## Links

[GitHub](https://github.com/devwithmj/quickbooks-mcp) · [NuGet package](https://www.nuget.org/packages/DevWithMJ.QuickBooks.Mcp) · [Releases](https://github.com/devwithmj/quickbooks-mcp/releases) · [Guide](https://github.com/devwithmj/quickbooks-mcp/blob/main/docs/GUIDE.md)

Requires Windows with QuickBooks Desktop installed. On other systems the launcher exits with an explanation.
