# User guide

For QuickBooks Desktop (Pro, Premier, Enterprise) on Windows. The server runs on the same computer as QuickBooks, started by your AI app, and reads the company file through QuickBooks' own SDK. Nothing is uploaded anywhere by this server; what the AI app sees is what the tools return (see [SECURITY-MODEL.md](SECURITY-MODEL.md)).

## 1. Install

Pick one.

| Way | Command / action | Needs |
|---|---|---|
| Claude Desktop extension | Open the `.mcpb` from the [latest release](https://github.com/devwithmj/quickbooks-mcp/releases) | Claude Desktop |
| npx | `npx -y @devwithmj/quickbooks-mcp` | Node.js 18+ |
| dnx | `dnx DevWithMJ.QuickBooks.Mcp --yes` | .NET 10 |
| Single file | Download `quickbooks-mcp-win-x64.exe` from the release | nothing |

Use the `win-x86` build if `--doctor` (below) says QuickBooks only registers a 32-bit component. This is common with QuickBooks before 2022.

## 2. Allow access in QuickBooks (once per company file)

QuickBooks refuses programs it has not been told to trust. Do this once:

1. Open QuickBooks and the company file **as a QuickBooks Admin**, in **single-user mode** (File > Switch to Single-user Mode).
2. Run the server once with `--authorize` (for example `quickbooks-mcp.exe --authorize`, or add `--authorize` to the npx command).
3. QuickBooks shows a dialog about "QuickBooks MCP". Choose **Yes, always; allow access even if QuickBooks is not running**, select an Admin user, and confirm.
4. The command prints the QuickBooks version and the qbXML versions it supports. You can switch back to multi-user mode.

You can review or revoke this later in QuickBooks under Edit > Preferences > Integrated Applications > Company Preferences.

## 3. Connect your AI app

Set `QB_COMPANY_FILE` to the full path of the `.qbw` file, or leave it out to use whichever file is open in QuickBooks.

**Claude Desktop** (`%APPDATA%\Claude\claude_desktop_config.json`) or the extension settings screen:

```json
{
  "mcpServers": {
    "quickbooks": {
      "command": "npx",
      "args": ["-y", "@devwithmj/quickbooks-mcp"],
      "env": { "QB_COMPANY_FILE": "C:\\Books\\Company.qbw" }
    }
  }
}
```

**Claude Code:**

```
claude mcp add quickbooks -e QB_COMPANY_FILE="C:\Books\Company.qbw" -- npx -y @devwithmj/quickbooks-mcp
```

**VS Code** (`.vscode/mcp.json`, or the user `mcp.json`):

```json
{
  "servers": {
    "quickbooks": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@devwithmj/quickbooks-mcp"],
      "env": { "QB_COMPANY_FILE": "C:\\Books\\Company.qbw" }
    }
  }
}
```

**Cursor** (`.cursor/mcp.json` or `~/.cursor/mcp.json`): same shape as Claude Desktop.

**Anything else:** the command is `npx -y @devwithmj/quickbooks-mcp` (or the `.exe` path), speaking MCP over stdio. To use the downloaded exe directly, set `"command"` to its path and drop `args`.

## 4. Try it

- "What is in my chart of accounts?"
- "Show accounts receivable aging, and the three customers who owe the most."
- "Find invoices to Acme from last quarter."
- "Profit and loss for last month by class, as a table."

Tools and their parameters are listed in [TOOLS.md](TOOLS.md).

## 5. Turning on changes

Everything is read-only until you opt in. In the same `env` (or as arguments):

| Setting | Effect |
|---|---|
| `QB_ALLOW_WRITES=true` / `--allow-writes` | enable create, update, delete and void |
| `QB_ENABLE_WRITE`, `QB_ENABLE_UPDATE`, `QB_ENABLE_DELETE` | enable one kind only |
| `QB_WRITE_ALLOW=Invoice,Customer` | limit which record types can change |
| `QB_CONFIRM_WRITES=true` | also ask you to approve every create and update (deletes and voids are always confirmed) |
| `QB_ALLOW_PAYROLL=true` | allow payroll data and reports |
| `QB_REDACT=false` | stop masking tax IDs and bank and card numbers (not recommended) |

Changes support a preview (`dryRun`), deletes and voids need a confirmation step, and everything is logged to `%LOCALAPPDATA%\quickbooks-mcp\audit.jsonl`. Details: [WRITES.md](WRITES.md). Other settings: `QB_MODE` (`single`, `multi` or `any`), `QB_TIMEOUT_SECONDS`, `QB_MAX_RETRIES`, `QB_CACHE_SECONDS` (see [PERFORMANCE.md](PERFORMANCE.md)), `QB_LOG_LEVEL`, `QB_CONFIG` (a JSON file with the same names in lowercase). Command-line flags beat environment variables, which beat the file.

## 6. Troubleshooting

Run `quickbooks-mcp.exe --doctor` (or `npx -y @devwithmj/quickbooks-mcp --doctor`). It prints the process bitness, whether QuickBooks' request processor is registered, the company file, and tries a connection.

| What you see | What it means | Fix |
|---|---|---|
| `QBXMLRP2 : NOT registered` | QuickBooks Desktop is not installed here, or only the other bitness is | Install QuickBooks; try the `win-x86` build |
| `Authorization : FAILED - The requested connection type could not be found` | QuickBooks cannot be reached through the SDK; most often QuickBooks Desktop itself is not installed on this computer | Install and open QuickBooks Desktop |
| "has not accessed this QuickBooks company data file before" | The app was never authorized for this file | Do step 2 (`--authorize`) as an Admin |
| "A dialog is open in QuickBooks" | A modal window, an update prompt or a login screen is waiting | Close it and try again |
| "...single-user mode ... another application" | Another program holds the file in single-user mode | Close it, or switch to multi-user |
| "could not be started" / timeouts | QuickBooks is closed, busy updating or very slow | Open QuickBooks and the file; raise `QB_TIMEOUT_SECONDS` |
| Company file `NOT FOUND` | Wrong path in `QB_COMPANY_FILE` | Use the full path including `.qbw` |

QuickBooks and the server must run at the same Windows privilege level. `--doctor` has a "Run as admin" line and warns about a mismatch. If QuickBooks runs "as administrator", start the AI app as administrator too (or the reverse).

## 7. FAQ

**Does QuickBooks have to be open?** Not if you chose "allow access even if QuickBooks is not running" and set `QB_COMPANY_FILE`; the server starts QuickBooks in the background for each request. With QuickBooks closed the first request is slow, and it cannot work while another user has the file in single-user mode.

**Can two AI apps (or two copies) use the same file at once?** Yes. Requests to one company file are serialized across processes with a lock, each in its own short session, so they take turns.

**Does it work with the file in multi-user mode?** Yes, with the file hosted normally. Writes need the permissions of the QuickBooks user you authorized. `QB_MODE=multi` forces multi-user opening; `any` (default) accepts whatever mode the file is in.

**Can it run as a Windows service or on a server with nobody logged in?** No. QuickBooks Desktop's SDK needs an interactive user session. See [the remote-use note](design/events-and-web-connector.md).

**QuickBooks Online?** Not this project. Intuit publishes a separate server for Online.

**Where is my data sent?** Only to the AI app you connected, as tool results. The server has no telemetry and makes no network calls (the npx launcher downloads the release once).

**How do I undo a change?** Look in the audit log for the record IDs, and fix them in QuickBooks (a voided transaction stays visible at zero). There is no automatic rollback; that is why changes are off by default.

## 8. Logs and bug reports

- **Logs** go to stderr (your AI app usually shows them in its MCP log view). Set `QB_LOG_LEVEL` (`Trace` ... `Critical`) and optionally `QB_LOG_FILE` to also write to a file. Logs say which request ran, how long it took and whether it worked, with QuickBooks error codes. They never contain the request or reply, so no company data, names or amounts. Detailed error text appears only at `Debug` level.
- **Bug reports:** run `--diagnostics` and paste the output. It has versions, your settings (the company file by name only), and the `--doctor` checks. It contains no company data.
- **No telemetry.** Nothing is sent anywhere unless a future version asks you to opt in first.

## 9. Multi-currency and older company files

In a multi-currency company, each transaction carries its own currency (`CurrencyRef`) and exchange rate. Any search result that contains a foreign-currency record gets a `currency` block naming the home currency and how to read amounts: amounts on a record are in that record's currency, fields ending in `InHomeCurrency` are in the home currency, and reports are always in the home currency. Single-currency companies get no such block (and no extra request). Company files created in older QuickBooks years work as long as the installed QuickBooks can open them; features newer than that QuickBooks are refused with a message (see [COMPATIBILITY.md](COMPATIBILITY.md)).
