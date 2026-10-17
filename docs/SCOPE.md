# Scope and non-goals

## What this is
An MCP server for **QuickBooks Desktop** (Pro, Premier, Enterprise) on **Windows**, running locally next to QuickBooks and talking to it through its SDK request processor (qbXML). Read by default; changes only when switched on.

## What it is not

| Out of scope | Why | What to use |
|---|---|---|
| **QuickBooks Online** | Different API, auth and data model. Intuit publishes an official Apache-2.0 MCP server for it; we learn from its design but do not duplicate it. | Intuit's QuickBooks Online MCP server |
| **QuickBooks for Mac** | It has no SDK to talk to. | Export data, or Desktop for Windows |
| **Credit card processing** | Card data would put this server and the model's transcripts in PCI scope. Decided and enforced in code. | QuickBooks Payments and merchant tools. See [the decision](design/credit-card-processing.md) |
| **Running as a Windows service or on a server with no session** | The QuickBooks Desktop SDK needs an interactive user session. | Run in the logged-in user's session. A remote mode through the Web Connector is a possible later, separate component ([the note](design/events-and-web-connector.md)) |
| **Hosting company files, syncing to a cloud, or storing your data** | The server keeps no copy; it reads and writes through QuickBooks each time. | A backup or sync tool |
| **Replacing QuickBooks' own checks** | QuickBooks decides what is valid and what the authorized user may do. | n/a |
| **Telemetry or analytics** | None, and none will be added by default. | n/a |

## Design commitments
- Local, stdio, started by the client; no network listener.
- qbXML through `QBXMLRP2`, not the QBFC interop; no Intuit binaries are redistributed.
- Read-only unless switched on, then previews, confirmations and an audit log. See the [security model](SECURITY-MODEL.md).
- Few, predictable tools rather than one per entity. See the [tool guidelines](TOOL-GUIDELINES.md).
