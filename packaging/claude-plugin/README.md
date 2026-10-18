# QuickBooks Desktop

Ask questions about your QuickBooks Desktop books (Pro, Premier or Enterprise) on Windows: customers, vendors, items, invoices, bills, reports and more. This is for QuickBooks Desktop, not QuickBooks Online.

## Use it

1. Install the plugin and choose your company file when asked, or leave it empty to use the file that is open in QuickBooks.
2. Open the company file in QuickBooks as an Admin, in single-user mode, and run the server once with `--authorize` so QuickBooks trusts "QuickBooks MCP". Step-by-step instructions are in the [setup guide](https://github.com/devwithmj/quickbooks-mcp/blob/main/docs/GUIDE.md).
3. Ask, for example, "What is my A/R aging?" or "List the unpaid bills due this month."

The server is read-only by default. Creating, changing, deleting and voiding records are separate switches with previews, confirmation tokens and an audit log.

## What it runs

The plugin installs `@devwithmj/quickbooks-mcp` 0.1.4 from the npm registry at the exact version pinned in `package-lock.json`, then starts it with `node`. On first start it downloads a checksummed Windows executable from the project's GitHub release. The program talks to the QuickBooks installed on your computer through its local SDK.

## Privacy Policy

The server collects no data, has no telemetry and opens no network connections apart from the one-time download of its own package and executable. Results go only to your AI app. It keeps a small local audit log and duplicate-prevention ledger in `%LOCALAPPDATA%\quickbooks-mcp`. Read the full [privacy policy](https://github.com/devwithmj/quickbooks-mcp/blob/main/docs/PRIVACY.md).

## Support

Report problems at https://github.com/devwithmj/quickbooks-mcp/issues. MIT licensed.
