# quickbooks-mcp

Open-source [MCP](https://modelcontextprotocol.io) server that connects **QuickBooks Desktop** (Pro, Premier, Enterprise) to LLMs and agents.

> Status: early scaffolding. Not usable yet.

## Planned

- Read-only queries and reports by default; writes only with `--allow-writes`.
- Runs over stdio in the user's own Windows session (QuickBooks SDK cannot run as a Windows Service).
- One-time per-company-file authorization in QuickBooks (`--authorize`).
- Install via `npx`, `dnx`, the MCP Registry, or a one-click `.mcpb` bundle.

## Requirements

- Windows, QuickBooks Desktop installed and licensed.
- Admin access to the company file for the one-time authorization.

## License

MIT
