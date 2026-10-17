# Building and running from source

For contributors, and for anyone who wants to run an unreleased build. If you only want to use the server, install a release instead ([user guide](GUIDE.md)).

## What you need
- Windows 10 or 11.
- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` should print `10.x`).
- Git.
- QuickBooks Desktop installed, only to run against a real company file. Building and the unit tests need no QuickBooks.
- Node.js 18+, only to try the npm launcher or the `.mcpb` bundle locally.

## 1. Get the code and build
```
git clone https://github.com/devwithmj/quickbooks-mcp.git
cd quickbooks-mcp
dotnet build
dotnet test
```
`dotnet test` runs the unit tests against recorded responses; the integration tests are skipped unless `QB_COMPANY_FILE` is set (see step 4).

## 2. Run from source
```
dotnet run --project src/QuickBooks.Mcp.Server -- --doctor
```
Everything after `--` goes to the server. The useful flags are:

| Flag | Does |
|---|---|
| `--doctor` | Checks bitness, that QuickBooks' request processor is registered, the company file, and tries a connection |
| `--authorize` | One-time approval of this app in QuickBooks (see below) |
| `--diagnostics` | Prints a report for bug reports; no company data |
| `--allow-writes`, `--allow-payroll` | Turn on changes or payroll data (off by default; see [WRITES.md](WRITES.md)) |

With no flag the server speaks MCP over stdio, which is what an AI app starts.

## 3. Allow access in QuickBooks (once per company file)
1. Open QuickBooks and the company file as an Admin, in single-user mode, with no dialog open.
2. Set the file and authorize:
   ```
   set QB_COMPANY_FILE=C:\path\to\company.qbw
   dotnet run --project src/QuickBooks.Mcp.Server -- --authorize
   ```
   (PowerShell: `$env:QB_COMPANY_FILE="C:\path\to\company.qbw"`.)
3. In the QuickBooks dialog choose **Yes, always; allow access even if QuickBooks is not running**, pick an Admin user and confirm.
4. Run `--doctor` again; `Authorization` should say OK.

Use Intuit's sample company file, or a copy, for development. Never use real data and never commit a company file.

QuickBooks and the server must run at the same Windows privilege level (both normal or both as administrator). `--doctor` warns about a mismatch.

## 4. Run the integration tests
```
set QB_COMPANY_FILE=C:\path\to\sample.qbw
dotnet test --filter FullyQualifiedName~QuickBooksIntegrationTests
```
They are read-only. `dotnet run` and `dotnet test` use the bitness of your installed .NET. If QuickBooks only registers a 32-bit request processor (common before QuickBooks 2022), install the x86 .NET SDK or use a published `win-x86` build (next step) with `--doctor`.

## 5. Build the single-file executables
```
dotnet publish src/QuickBooks.Mcp.Server -c Release -r win-x64 -p:Version=0.0.0-local -o out/win-x64
dotnet publish src/QuickBooks.Mcp.Server -c Release -r win-x86 -p:Version=0.0.0-local -o out/win-x86
```
Each `quickbooks-mcp.exe` is about 75 MB and needs no .NET on the target machine. Copy one to the QuickBooks computer and run `--doctor`.

## 6. Connect an AI app to your build
Point the app's MCP configuration at the executable (or at `dotnet run`):

```json
{
  "mcpServers": {
    "quickbooks-dev": {
      "command": "C:\path\to\out\win-x64\quickbooks-mcp.exe",
      "args": [],
      "env": { "QB_COMPANY_FILE": "C:\path\to\sample.qbw" }
    }
  }
}
```
Add `"--allow-writes"` to `args` only for a throwaway copy of a company file.

## 7. Try the other packages locally
| Package | Local build and run |
|---|---|
| npm launcher | `set QB_MCP_EXE=C:\path\to\quickbooks-mcp.exe` then `node packaging/npm/bin/quickbooks-mcp.js --doctor`; unit tests: `cd packaging/npm && npm test` |
| NuGet / dnx | `dotnet pack src/QuickBooks.Mcp.Server -c Release -p:Version=0.0.0-local -o nupkg`, then `dotnet dnx DevWithMJ.QuickBooks.Mcp --version 0.0.0-local --source nupkg --yes -- --doctor` |
| `.mcpb` bundle | Put `packaging/mcpb/manifest.json` (replace `__VERSION__`), the published exe as `server/quickbooks-mcp.exe`, `README.md` and `LICENSE` in one folder, then `npx -y @anthropic-ai/mcpb validate manifest.json` and `npx -y @anthropic-ai/mcpb pack <folder> out.mcpb` |

Releases are cut by pushing a tag; see [RELEASING.md](RELEASING.md).

## Troubleshooting
See the table in the [user guide](GUIDE.md#6-troubleshooting). The most common development problem is `Authorization : FAILED - The requested connection type could not be found`: QuickBooks Desktop is not installed on this computer, or the bitness of your build does not match what QuickBooks registered.
