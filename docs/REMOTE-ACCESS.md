# Advanced: connect from outside the computer

The server runs on the computer that has QuickBooks and speaks MCP over stdio, so a client on the same computer needs nothing more than the setup in [GUIDE.md](GUIDE.md). This page is for people who want a client that runs elsewhere (a chat app in the browser, a phone, another machine) to reach **their own** books through a private link.

There is no hosted service and none is planned. Each person runs a tunnel on their own computer; the link is theirs, and the data never goes anywhere except to the client they connect.

## Read this first

- Exposing a company file is a real risk. Keep **writes off** (the default), and never use a link that has no login.
- The computer must stay on, signed in, with QuickBooks and the company file open.
- Treat the link, keys and tokens below like a password. Rotate them if they leak.
- This is for your own use. A tunnel cannot be used to list the server in a public directory, which needs a public service run by the publisher.

## Option 1: OpenAI's secure tunnel (for ChatGPT)

OpenAI's tunnel needs no public address: a small client on your computer makes outbound connections only, and it can start the server for you. Requirements and limits are in [OpenAI's guide](https://developers.openai.com/api/docs/guides/secure-mcp-tunnels); check it for current plan and permission rules.

1. In the OpenAI Platform, open [tunnel settings](https://platform.openai.com/settings/organization/tunnels), create a tunnel, and note its `tunnel_id`. Create an API key for the tunnel client.
2. Download `tunnel-client` from the [latest release](https://github.com/openai/tunnel-client/releases/latest).
3. Point it at the server (adjust the paths):

```powershell
$env:CONTROL_PLANE_API_KEY = "sk-..."
tunnel-client init --sample sample_mcp_stdio_local --profile quickbooks `
  --tunnel-id tunnel_0123456789abcdef0123456789abcdef `
  --mcp-command "C:\Tools\quickbooks-mcp.exe"
tunnel-client doctor --profile quickbooks --explain
tunnel-client run --profile quickbooks
```

   Put `QB_COMPANY_FILE` in the environment first if you want a specific file. Leave the client running.
4. In ChatGPT, open Plugins, choose the plus button, then **Add custom MCP server**, set **Connection** to **Tunnel**, and pick your tunnel.

OpenAI limits a tunnel to 600 requests a minute and 120 seconds per request, which is plenty for this server.

## Option 2: the built-in HTTP mode with a Cloudflare tunnel (any client)

Use this for clients that accept a remote MCP address. The server can speak MCP over HTTP itself, so there is no bridge to install. It listens only on this computer (`127.0.0.1`) unless you pass `--http-bind 0.0.0.0` (LAN — see [DEPLOY.md](DEPLOY.md)), and every request must carry an access key.

1. **Start it.** The first run makes a random key and saves it next to the audit log; later runs reuse it.

```powershell
$env:QB_COMPANY_FILE = "C:\Books\Company.qbw"
C:\Tools\quickbooks-mcp.exe --http
```

   It prints the address (`http://127.0.0.1:8765/mcp`) and the key. Use `--http-port 9000` to change the port, or set `QB_HTTP_KEY` (at least 16 characters) to choose your own key. Changes are refused in this mode unless you also pass `--http-allow-writes`.
2. **Tunnel.** The address above only works on this computer. To reach it from elsewhere, put a tunnel in front of it. The quickest way to try it is a Cloudflare quick tunnel, which needs no account: install `cloudflared` (`winget install Cloudflare.cloudflared`) and run `cloudflared tunnel --url http://127.0.0.1:8765`. It prints a random `https://...trycloudflare.com` address that changes every time you start it. For a permanent address on your own domain, create a named tunnel as described in [Cloudflare's tunnel setup](https://developers.cloudflare.com/tunnel/setup/) and run it as a Windows service.
3. **Login in front (recommended for a permanent address).** The access key already stops anyone without it, but a [Cloudflare Access](https://developers.cloudflare.com/cloudflare-one/access-controls/ai-controls/secure-mcp-servers/) policy adds a second lock. Clients that cannot sign in through a browser can use an Access service token (the `CF-Access-Client-Id` and `CF-Access-Client-Secret` headers).
4. **Client.** Add `https://<your-tunnel-address>/mcp` as a remote MCP server and send the key as a bearer token (`Authorization: Bearer <key>`) or as an `X-API-Key` header, whichever the client offers.

Most people never need this. The ChatGPT and Claude desktop apps, Claude Code, editors and the Gemini command line start the server themselves, as described in the [guide](GUIDE.md).

## Checking it works

Ask for something harmless ("What is in my chart of accounts?"). Then try the link from another network without the key or login: it must be refused. If it is not, shut the tunnel down.
