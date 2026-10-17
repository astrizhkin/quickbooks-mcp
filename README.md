# quickbooks-mcp

Open-source [MCP](https://modelcontextprotocol.io) server that connects **QuickBooks Desktop** (Pro, Premier, Enterprise) to LLMs and agents.

> Status: pre-1.0. Built and tested against recorded responses and the qbXML schema; not yet validated across QuickBooks versions (see [COMPATIBILITY.md](docs/COMPATIBILITY.md)).

## What you get

- **Read:** customers, vendors, employees, items, accounts, invoices, bills, payments, inventory, time, custom fields, and every standard report (aging, P&L, balance sheet, ...), as compact tables.
- **Change, only if you allow it:** create, update, delete and void, with previews, confirmation tokens, idempotency keys, batches and an audit log.
- **Safe by default:** read-only, tax IDs and bank and card numbers masked, payroll off, card data never accepted.
- **Local:** runs on your Windows PC next to QuickBooks and talks to it through its SDK. No cloud, no telemetry.

## Install

| | |
|---|---|
| Claude Desktop | open the `.mcpb` from the [latest release](https://github.com/devwithmj/quickbooks-mcp/releases) |
| npx | `npx -y @devwithmj/quickbooks-mcp` |
| dnx | `dnx DevWithMJ.QuickBooks.Mcp --yes` |
| exe | `quickbooks-mcp-win-x64.exe` (or `-win-x86`) from the release |

Then allow access once in QuickBooks (`--authorize`) and add the server to your AI app. Step by step: **[docs/GUIDE.md](docs/GUIDE.md)**.

## Requirements

- Windows with QuickBooks Desktop installed and licensed.
- Admin access to the company file for the one-time authorization.

Not for QuickBooks Online (Intuit has its own MCP server), QuickBooks for Mac (no SDK), card processing, or running as a Windows service. See [docs/SCOPE.md](docs/SCOPE.md).

## Documentation

- [User guide](docs/GUIDE.md): install, authorize, connect clients, troubleshoot, FAQ
- [Tools](docs/TOOLS.md): every tool, parameter and example
- [Writes](docs/WRITES.md) and [Reports](docs/REPORTS.md)
- [Scope and non-goals](docs/SCOPE.md) and [security model](docs/SECURITY-MODEL.md)
- [Releasing](docs/RELEASING.md), [Testing](docs/TESTING.md), [Contributing](CONTRIBUTING.md)

## Legal

Not affiliated with or endorsed by Intuit. See [NOTICE.md](NOTICE.md).

## License

MIT

<!-- mcp-name: io.github.devwithmj/quickbooks-mcp -->
