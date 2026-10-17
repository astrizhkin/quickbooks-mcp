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

## QuickBooks Online or Desktop?

This project is for **QuickBooks Desktop**. If you use **QuickBooks Online**, use Intuit's own server: [intuit/quickbooks-online-mcp-server](https://github.com/intuit/quickbooks-online-mcp-server) (Apache-2.0). We read its design for ideas and wrote our own code; no code was copied.

| | QuickBooks Online server (Intuit) | This project (Desktop) |
|---|---|---|
| Targets | QuickBooks Online | QuickBooks Desktop Pro, Premier, Enterprise (Windows) |
| Where it runs | Talks to Intuit's cloud API | On your PC, next to QuickBooks, through its local SDK |
| Setup and sign-in | An Intuit developer app and OAuth | A one-time "allow this app" approval inside QuickBooks (`--authorize`); no developer account |
| Only on Desktop | | Inventory assemblies and sites, price levels, workers comp codes, custom fields via data extensions, opening a record in the QuickBooks window |
| Only on Online | Attachments, invoice PDFs and other cloud features | |
| Safety default | See that project | Read-only until you enable writes; deletes and voids need your approval; sensitive numbers masked; payroll off; audit log |
| Data path | Through Intuit's cloud | Stays on your computer except what the assistant is shown |

The two do not share a company file or an account: a business on Online should not use this project, and a Desktop business cannot use Intuit's.

## Requirements

- Windows with QuickBooks Desktop installed and licensed.
- Admin access to the company file for the one-time authorization.

Not for QuickBooks Online (Intuit has its own MCP server), QuickBooks for Mac (no SDK), card processing, or running as a Windows service. See [docs/SCOPE.md](docs/SCOPE.md).

## Documentation

- [User guide](docs/GUIDE.md): install, authorize, connect clients, troubleshoot, FAQ
- [Tools](docs/TOOLS.md): every tool, parameter and example
- [Prompt cookbook](docs/COOKBOOK.md), [Writes](docs/WRITES.md) and [Reports](docs/REPORTS.md)
- [Scope and non-goals](docs/SCOPE.md) and [security model](docs/SECURITY-MODEL.md)
- [Roadmap](docs/ROADMAP.md) and [1.0 checklist](docs/RELEASE-1.0.md)
- [Support, upgrades and uninstall](docs/SUPPORT.md), [Compatibility](docs/COMPATIBILITY.md), [Performance](docs/PERFORMANCE.md)
- [Building from source](docs/BUILDING.md), [Releasing](docs/RELEASING.md), [Testing](docs/TESTING.md), [Contributing](CONTRIBUTING.md)

## Legal

Not affiliated with or endorsed by Intuit. See [NOTICE.md](NOTICE.md).

## License

MIT

<!-- mcp-name: io.github.devwithmj/quickbooks-mcp -->
