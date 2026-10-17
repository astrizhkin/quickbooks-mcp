# Spike: QuickBooks events and the Web Connector (remote or headless use)

Status: research note. Decision: **no-go for 1.0**; revisit after 1.0 if there is demand for remote or unattended use.

## The question
The server runs as a stdio process in the user's interactive Windows session, next to QuickBooks Desktop. Two things that does not cover:
1. Being told when data changes (instead of polling).
2. Using QuickBooks from somewhere other than the machine it runs on, or with nobody logged in.

## Options

### A. Event subscriptions (DataEventSubscription, UIEventSubscription)
QuickBooks can notify a registered application when records change (data events) or when the user does something in the UI (UI events).
- Notifications are delivered to a COM callback component registered on the same machine, and QuickBooks must be running. They are local only.
- A stdio MCP server is started and stopped by the client, so it is usually not running when an event fires. Catching events would need a separate always-on companion process (and a COM registration step), which is the opposite of the "start from the client, no installer" design.
- Events say that something changed, not what; the receiver must query afterwards.
- Verdict: poor fit. `get_changes_since` and `get_deleted_records` (#37) give the same result on demand with no extra moving parts.

### B. QuickBooks Web Connector (QBWC, SOAP)
QBWC is a small Windows program that runs on the QuickBooks machine and polls a web service you host. Each poll it can fetch qbXML requests, run them against the open (or auto-opened) company file, and post the responses back.
- Works across machines and networks (the connector dials out; no inbound ports on the QuickBooks machine). Can run unattended once authorized, but it is a user-session program, not a Windows service, so the machine still needs a logged-in session.
- Strictly asynchronous: requests wait for the next poll (the connector's minimum interval is minutes, not milliseconds). Interactive chat ("what is Acme's balance?") would feel broken; batch and sync jobs fit well.
- Needs a hosted SOAP endpoint (authenticate, sendRequestXML, receiveResponseXML, closeConnection, error handling), a request queue, per-company credentials, TLS, and a user-installed `.qwc` file. That is a service to run and secure, with company financial data passing through it.
- Verdict: a different product (a hosted bridge), with real operational and security cost.

### C. Keep local-only (status quo)
The server stays a local stdio process. Remote use is possible today by running an MCP client on the QuickBooks machine, or by exposing the local MCP server through the client's own remote mechanism.

## Recommendation
- 1.0: C. Document that the server is local and interactive only (already in the README and the scope doc).
- After 1.0, if asked for: build B as a separate, optional component that reuses Core (qbXML builder and parser, the gate, audit) behind a queue, with writes disabled by default. Do not add A.

## Things to verify before building B
- Actual minimum poll interval and whether it is configurable per company file.
- Behaviour when QuickBooks is closed and when a dialog is open (same blocking issue as the direct COM path).
- How the Intuit SDK license treats a hosted relay.
- Where the queue and its data live, and who is responsible for them.
