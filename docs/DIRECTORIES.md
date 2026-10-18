# Directory and marketplace submissions

Everything needed to list the server, kept in one place. Submissions are made by a maintainer from their own accounts; each directory's process changes, so check its current instructions first.

## Positioning
This is for **QuickBooks Desktop** (Pro, Premier, Enterprise) on Windows. It is not for QuickBooks Online; Intuit publishes a separate server for that. Say so first in every listing, because people searching for "QuickBooks MCP" mostly want Online.

## One-paragraph pitch
QuickBooks Desktop MCP server: ask Claude or any MCP client about the books on your own computer: customers, vendors, items, invoices, bills, reports and more. It talks to the installed QuickBooks through its local SDK, so no cloud account or data upload is involved. It is read-only by default; creating, changing, deleting and voiding records are separate switches with previews, confirmation tokens and an audit log, and tax IDs, bank and card numbers and payroll details are masked unless you opt in. Windows only. Install with `npx @devwithmj/quickbooks-mcp`, `dnx DevWithMJ.QuickBooks.Mcp`, or the one-click `.mcpb` bundle.

## Short description (100 characters)
Read QuickBooks Desktop data and, if enabled, make safe changes. Windows only.

## Facts for forms
| | |
|---|---|
| Name | QuickBooks Desktop (quickbooks-mcp) |
| Registry name | `io.github.devwithmj/quickbooks-mcp` |
| Repository | https://github.com/devwithmj/quickbooks-mcp |
| License | MIT |
| Transport | stdio, local |
| Platform | Windows (x64 and x86) |
| Install | `npx -y @devwithmj/quickbooks-mcp` · `dnx DevWithMJ.QuickBooks.Mcp --yes` · `.mcpb` bundle from the release |
| Tags | quickbooks, accounting, bookkeeping, finance, windows |
| Icon | `docs/assets/icon.svg` (a ledger with a plug; not Intuit's logo) |

## Checklist
Order matters: the official registry first, because several directories read from it. Status as of 2026-10-18.

- [x] **Official MCP Registry**: published by the release workflow (#44); version 0.1.5 is listed and active.
- [ ] **PulseMCP**: ingests from the official registry; not checked yet (the site refused an automated request). Check the listing in a browser and claim it if needed.
- [ ] **VS Code MCP gallery / GitHub MCP registry**: read the official registry; not checked yet.
- [x] **Glama**: claimed, and the description now says Desktop, Windows, not Online. Its discovery score is low because Glama tests servers in a Linux container and this one needs QuickBooks on Windows; that is expected.
- [x] **Claude directory (plugin)**: submitted from `packaging/claude-plugin`; waiting for review. The directory no longer takes `.mcpb` bundles for local servers.
- [x] **Claude Code marketplace**: this repository is one (`.claude-plugin/marketplace.json`); see the README for the two commands.
- [ ] **awesome-mcp-servers**: pull request open (punkpeye/awesome-mcp-servers#16864), waiting for the maintainers.
- [ ] **Intuit "Apps for QuickBooks Desktop"**: needs a technical review requested through an Intuit developer support ticket, then a developer account for the listing. Not sent yet.
- [ ] **Cursor directory**: not tried; needs a sign-in, and it is not clear it lists local servers.
- Skipped: **Smithery** (listing needs a server URL, and this server runs on the user's own PC), **mcp.so** (only a paid submission was offered), the **OpenAI plugin directory** (local servers there are limited to approved partners; see [GUIDE.md](GUIDE.md) and [REMOTE-ACCESS.md](REMOTE-ACCESS.md) for using the server from ChatGPT desktop or over a private link).

## Screenshots to capture (needs a QuickBooks install; use Intuit's sample company file)
1. Claude Desktop answering "What is my A/R aging?" with the table.
2. The extension settings screen showing company file, allow changes (off), payroll (off), redaction (on).
3. A write preview: `dryRun` request and the confirmation prompt for a void.
4. `--doctor` output on a healthy machine.

Never capture a real company file; use the sample company.
