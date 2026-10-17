# Support, upgrades and uninstall

## Getting help
1. Run `--doctor` and read the table in the [guide](GUIDE.md); most problems are authorization, a dialog open in QuickBooks, or a bitness mismatch.
2. Ask in GitHub Discussions (Q&A), or open an issue using the bug template. Paste the output of `--diagnostics`; it contains no company data. Never post a company file or real records.
3. Security problems go through [SECURITY.md](../SECURITY.md), not public issues.

This is a volunteer project. There is no service-level promise; maintainers aim to look at new issues within about a week ([triage guide](TRIAGE.md)).

## What is supported
| | Policy |
|---|---|
| **Server releases** | Only the latest release gets fixes before 1.0. After 1.0, the latest minor of the current major, plus security fixes for the previous minor for 90 days. |
| **QuickBooks Desktop** | Pro, Premier and Enterprise versions that Intuit still supports and that offer the SDK request processor. The server uses the newest qbXML version the installed QuickBooks understands (up to 17.0) and refuses features it is too old for with a clear message. What has actually been tested is in [COMPATIBILITY.md](COMPATIBILITY.md); the rest is expected to work but is not promised. |
| **Not supported** | QuickBooks Online, Mac, running as a service. See [SCOPE.md](SCOPE.md). |
| **Windows / Node / .NET** | Windows 10 and 11 (and the matching Server editions). The npm launcher needs Node 18 or newer; the NuGet route needs .NET 10; the executable and the bundle need neither. |
| **MCP clients** | Any that speak MCP over stdio. Clients that support progress and elicitation get a better experience; others fall back automatically. |

When a QuickBooks version passes out of Intuit's support, it moves to "best effort" here and is noted in COMPATIBILITY.md rather than removed abruptly.

## Upgrading
- **Install route:** `npx -y` always fetches the latest; pin with `npx -y @devwithmj/quickbooks-mcp@0.2.0`. `dnx DevWithMJ.QuickBooks.Mcp --version 0.2.0`. For the exe or the bundle, download the new release and replace the old one (or open the new `.mcpb`; Claude Desktop updates the extension).
- **Settings:** they are environment variables and arguments, so there is nothing to migrate. Before 1.0, a renamed or removed setting is listed under "breaking" in the release notes. Unknown settings in the JSON config file are ignored.
- **QuickBooks authorization:** QuickBooks remembers the app by its application name, **QuickBooks MCP**. That name never changes (a test pins it), so upgrading does not ask you to authorize again. If it ever had to change, the release notes would say so and `--authorize` would be needed once.
- **Your data:** nothing in the company file changes on upgrade. The audit log and idempotency ledger (`%LOCALAPPDATA%\quickbooks-mcp`) are append-only text files that newer versions keep reading.
- **Rolling back:** install the older version. Newer tools simply disappear; nothing is left behind in QuickBooks.

## Uninstall
1. **Remove it from your AI app:** delete the `quickbooks` entry from its MCP configuration (or remove the extension in Claude Desktop).
2. **Revoke access in QuickBooks** (do this one; removing the app alone does not): open the company file as Admin, then *Edit > Preferences > Integrated Applications > Company Preferences*, select **QuickBooks MCP** and remove it. Do it for each company file you authorized.
3. **Delete local files** (optional):
   - `%LOCALAPPDATA%\quickbooks-mcp`: the audit log, the idempotency ledger, and the launcher's cached executables.
   - The npx cache (`npm cache clean --force` clears it) or the NuGet tool cache (`dotnet nuget locals all --clear`), if you used those routes.
   - The downloaded `.exe`, if you used that route.
4. Nothing was installed system-wide, no service was registered, and no registry entries were added by the server itself, so there is nothing else to remove.
