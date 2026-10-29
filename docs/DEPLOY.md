# Deploy and launch: step by step

A minimal runbook for running the server yourself. For what the tools do and the write
switches see [GUIDE.md](GUIDE.md); for reaching the books over the internet see
[REMOTE-ACCESS.md](REMOTE-ACCESS.md).

## 0. Check the machine

```
quickbooks-mcp.exe --doctor
```

Shows the QuickBooks product and qbXML versions, whether the request processor is
registered (and its bitness — use the `win-x86` build if only 32-bit is registered),
and the authorization state.

**Hard rule:** the server must run in the **same interactive Windows session** that runs
QuickBooks. A service, a scheduled task in session 0, or an SSH-spawned process cannot
talk to QuickBooks.

## 1. Authorize (once per company file)

1. Open QuickBooks and the company file as a QuickBooks **Admin**, in **single-user** mode.
2. Run `quickbooks-mcp.exe --authorize`.
3. In the QuickBooks dialog choose **Yes, always; allow access even if QuickBooks is not
   running**, pick the user, confirm.
4. `--doctor` should now print `Authorization : OK`.

Revoke later under Edit > Preferences > Integrated Applications.

## 2. The QuickBooks user

The SDK connects as the **Windows user** running the server; there is no `QB_USER`
setting. Two places a QuickBooks *user* matters:

- **Authorization** (step 1) grants access to the QuickBooks user you pick in the dialog.
- **Multi-user mode:** when the file is opened in multi-user mode, QuickBooks may show a
  login dialog. Create a dedicated QuickBooks user for the integration (Company > Set Up
  Users & Roles, or straight from the dialog) and enter it there. While **any modal
  dialog is open in QuickBooks, every SDK request fails** — dismiss dialogs before
  testing.

## 3. Start the server

**Stdio (default)** — the AI client starts the process itself; you rarely start it by
hand. `QB_COMPANY_FILE` picks the file, or leave it unset to use the file open in
QuickBooks.

**HTTP mode** — one long-running process serving MCP over Streamable HTTP:

```
quickbooks-mcp.exe --http                      # loopback only (default)
quickbooks-mcp.exe --http --http-bind 0.0.0.0  # every network (see section 5)
```

It prints the address (`http://<bind>:8765/mcp`) and the access key.

## 4. Access key (HTTP mode)

- No key configured: a random one is generated and saved to
  `%LOCALAPPDATA%\quickbooks-mcp\http-key.txt`; later runs reuse it.
- Your own key: `--http-key <value>` or `QB_HTTP_KEY` — at least 16 characters.
- Every request must carry it: `Authorization: Bearer <key>` or `X-API-Key: <key>`.
  Wrong or missing key → `401`.
- Treat the key like a password; rotate it by editing the key file and restarting.

## 5. Listen address and the LAN

| Need | Do |
|---|---|
| Only this computer (default) | `--http` — binds `127.0.0.1:8765` |
| Different port | `--http-port 9000` or `QB_HTTP_PORT` |
| Whole LAN | `--http-bind 0.0.0.0` or `QB_HTTP_BIND=0.0.0.0` |
| One interface | `--http-bind 192.168.1.20` |

Opening the LAN needs a firewall rule (run from an **elevated** prompt):

```
netsh advfirewall firewall add rule name="quickbooks-mcp" dir=in action=allow protocol=TCP localport=8765
```

Notes:

- A **Block** rule for the same port/profile silently wins over Allow. If connections
  time out, check: `netsh advfirewall firewall show rule name=quickbooks-mcp`.
- On `0.0.0.0` the only gate is the access key. Keep it off the public internet — for
  that use a tunnel ([REMOTE-ACCESS.md](REMOTE-ACCESS.md)) instead.
- Writes in HTTP mode need both `--allow-writes` **and** `--http-allow-writes`.

## 6. Connect an agent

Any client that speaks MCP works — over stdio (it starts the exe) or over HTTP
(you started the exe; the client dials the URL).

**Claude Code**

```
claude mcp add quickbooks -- npx -y @devwithmj/quickbooks-mcp          # stdio
claude mcp add --transport http quickbooks http://192.168.1.20:8765/mcp \
  --header "Authorization: Bearer <key>"                               # HTTP
```

**Codex** (`codex mcp add quickbooks -- C:\Tools\quickbooks-mcp.exe`, or
`~/.codex/config.toml`):

```toml
[mcp_servers.quickbooks]
url = "http://192.168.1.20:8765/mcp"
http_headers = { Authorization = "Bearer <key>" }
```

**Any other harness** (DeepSeek, Open WebUI, Windsurf, Cline, …): add a *remote/streamable
HTTP* MCP server with URL `http://<host>:<port>/mcp` and the bearer (or `X-API-Key`)
header. If the harness only does stdio, point it at the exe instead — see the client
examples in [GUIDE.md](GUIDE.md).

**Smoke test from any shell:**

```
curl -X POST http://<host>:8765/mcp -H "Authorization: Bearer <key>" \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
```

A `serverInfo` reply means the transport, key and firewall are all right.

## 7. Configuration

Precedence: command line > `QB_*` environment > JSON config file (`--config path` or
`QB_CONFIG`). Same names everywhere: `HTTP_BIND`, `--http-bind`, `"http_bind"`.

```json
{
  "company_file": "C:\\Books\\Company.qbw",
  "mode": "multi",
  "http": true,
  "http_bind": "0.0.0.0",
  "http_port": 8765,
  "allow_writes": true,
  "http_allow_writes": true,
  "write_allow": "Invoice,Customer"
}
```

Common keys: `COMPANY_FILE`, `MODE` (any/single/multi), `HTTP`, `HTTP_BIND`,
`HTTP_PORT`, `HTTP_KEY`, `HTTP_ALLOW_WRITES`, `ALLOW_WRITES`, `WRITE_ALLOW`,
`CONFIRM_WRITES`, `AUDIT_LOG`, `LOG_LEVEL`, `LOG_FILE`. Full list in
[GUIDE.md](GUIDE.md) section 5.
