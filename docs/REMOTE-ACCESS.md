# Advanced: connect from outside the computer

The server runs on the computer that has QuickBooks and speaks MCP over stdio, so a client on the same computer needs nothing more than the setup in [GUIDE.md](GUIDE.md). This page is for people who want a client that runs elsewhere (a chat app in the browser, a phone, another machine) to reach **their own** books through a private link.

There is no hosted service and none is planned. Each person runs a small tunnel on their own computer; the link is theirs, and the data never goes anywhere except to the client they connect.

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

## Option 2: your own link with Cloudflare Tunnel (any client)

Use this for clients that accept a remote MCP address. It needs a free Cloudflare account and a domain on Cloudflare. It has three parts: a bridge that turns stdio into HTTP, a tunnel, and a login in front of it.

1. **Bridge.** [supergateway](https://github.com/supercorp-ai/supergateway) serves a stdio server over Streamable HTTP. Listen on the local loopback only, and require a key:

```powershell
$env:QB_COMPANY_FILE = "C:\Books\Company.qbw"
npx -y supergateway --stdio "C:\Tools\quickbooks-mcp.exe" `
  --outputTransport streamableHttp --port 8000 --host 127.0.0.1 `
  --apiKey "a-long-random-secret"
```

2. **Tunnel.** Install `cloudflared` and create a named tunnel that points a hostname on your domain at `http://localhost:8000`, as described in [Cloudflare's tunnel setup](https://developers.cloudflare.com/tunnel/setup/). Run it as a Windows service if you want it to survive a restart. Do not use a throwaway quick tunnel for books: it has no login of its own.
3. **Login in front.** Add a [Cloudflare Access](https://developers.cloudflare.com/cloudflare-one/access-controls/ai-controls/secure-mcp-servers/) policy for the hostname so only you can get through. Clients that cannot sign in through a browser can use an Access service token (the `CF-Access-Client-Id` and `CF-Access-Client-Secret` headers).
4. **Client.** Add the address `https://your-hostname/mcp` as a remote MCP server and send the key from step 1 as a bearer token (`Authorization: Bearer ...`) if the client has a field for it.

## Checking it works

Ask for something harmless ("What is in my chart of accounts?"). Then try the link from another network without the key or login: it must be refused. If it is not, shut the tunnel down.
