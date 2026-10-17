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
Writes are off unless you pass `--allow-writes`. See the [project README](https://github.com/devwithmj/quickbooks-mcp) for everything else.

Requires Windows with QuickBooks Desktop installed. On other systems the launcher exits with an explanation.
