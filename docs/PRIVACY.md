# Privacy policy

QuickBooks MCP is a local program. It reads and, only if you turn writes on, changes the QuickBooks Desktop company file on your own computer, and passes the results to the AI app that started it.

## Data collection
The server collects nothing about you. It has no accounts, no analytics and no telemetry, and it opens no network connections of its own. The optional `--http` mode listens only on this computer, behind an access key you control; sending that address anywhere else is your choice (see [REMOTE-ACCESS.md](REMOTE-ACCESS.md)).

## Use and storage
- Records and reports you ask for are returned to your AI app for that request only. The server keeps no copy.
- It writes two small local files in `%LOCALAPPDATA%\quickbooks-mcp`: an audit log of write attempts (time, operation, entity, record IDs; no field values) and a ledger that prevents duplicate creates. You can delete both at any time.
- Sensitive values such as tax IDs, bank and card numbers and payroll details are masked unless you turn masking off.

## Sharing with third parties
The project shares nothing. Once the server returns data to your AI app, that app and the model it uses handle the data under their own terms and privacy policy. Check them before connecting a company file that holds confidential books.

## Retention
The server holds no data between requests. The local audit log and ledger stay until you delete them.

## Contact
Questions or concerns: open an issue at https://github.com/devwithmj/quickbooks-mcp/issues. For security problems use the process in [SECURITY.md](../SECURITY.md).
