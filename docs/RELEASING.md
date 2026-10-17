# Releasing

Pushing a tag like `v0.2.0` runs `.github/workflows/release.yml`, which tests, then publishes two self-contained single-file builds
and attaches them to a GitHub release with checksums:

- `quickbooks-mcp-win-x64.exe`
- `quickbooks-mcp-win-x86.exe`
- `SHA256SUMS.txt`

Both architectures are shipped because QuickBooks registers its request processor (`QBXMLRP2`) per bitness: older QuickBooks versions
are 32-bit only, newer ones also register a 64-bit component. The launcher picks the build that matches (`--doctor` shows what is registered).

## Building locally
```
dotnet publish src/QuickBooks.Mcp.Server -c Release -r win-x64 -p:Version=0.2.0 -o out
```
The build is not trimmed (late-bound COM would break) so each file is about 75 MB. It needs no .NET runtime on the machine.

## Reproducibility
Builds are deterministic (`ContinuousIntegrationBuild` on CI, central package versions with lock files, pinned SDK major version).
To check a release, build the same tag on a Windows machine with the same SDK and compare SHA-256 of the executable. The GitHub release
also carries a build-provenance attestation (`gh attestation verify <file> --repo devwithmj/quickbooks-mcp`).

## Code signing
The executables are not signed yet because no certificate is available. Until they are, Windows SmartScreen may warn on first run;
verify the checksum and attestation instead. When a certificate exists, sign in the workflow after publish and before the checksum step.

## npm launcher
After the release exists, the `npm` job in the same workflow publishes `packaging/npm` as `@devwithmj/quickbooks-mcp`, with the release's
SHA-256 values written into `checksums.json` and npm provenance enabled. The launcher downloads the executable for its own version from the
GitHub release, refuses it unless it matches that checksum, caches it under `%LOCALAPPDATA%\quickbooks-mcp`, and runs it. It needs an
`NPM_TOKEN` repository secret. For development, `QB_MCP_EXE` points the launcher at a local build.

## NuGet package (dnx)
The `nuget` job packs `src/QuickBooks.Mcp.Server` as the .NET tool `DevWithMJ.QuickBooks.Mcp` (package types `DotnetTool` and `McpServer`,
with `.mcp/server.json` stamped with the release version) and pushes it to nuget.org; it needs a `NUGET_API_KEY` secret.

```
dnx DevWithMJ.QuickBooks.Mcp --yes
```

This runs on the machine's .NET 10 runtime, so its bitness is the machine's `dotnet`. If QuickBooks only registers a 32-bit request
processor, use the 32-bit .NET or the `win-x86` executable instead. Try a local package with
`dotnet pack -o nupkg` then `dnx DevWithMJ.QuickBooks.Mcp --version <v> --source nupkg --yes -- --doctor`.

## Desktop extension bundle (.mcpb)
The `mcpb` job packs `packaging/mcpb/manifest.json` with each executable into `quickbooks-mcp-win-x64.mcpb` and `quickbooks-mcp-win-x86.mcpb`
and adds them to the release. Install by opening the file in Claude Desktop (or dropping it on Settings > Extensions); the settings
screen asks for the company file and whether to allow changes, payroll data and redaction, which map to `QB_COMPANY_FILE`, `QB_ALLOW_WRITES`,
`QB_ALLOW_PAYROLL` and `QB_REDACT`. Use the x64 bundle unless QuickBooks is 32-bit only (`--doctor` shows what is registered).

Pack locally with `npx @anthropic-ai/mcpb pack <staging dir> <out.mcpb>` after copying the executable to `<staging>/server/quickbooks-mcp.exe`.
