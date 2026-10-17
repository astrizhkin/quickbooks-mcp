# Spike: working with more than one company file

Status: decided. **No-go for in-server file switching in 1.0.** One server entry per company file works today and is the documented way.

## The constraint
A QuickBooks Desktop instance has one company file open at a time. A request that names a different file than the one open in the QuickBooks window fails with QuickBooks' own error; a request when QuickBooks is closed (and the app is allowed to run unattended) opens the named file in the background for the session. So several files can be used one after another, but not at the same moment, and never while a person has another file open in the window.

## Options considered

### A. A list of files and a `select_company_file` tool (switching inside one server)
- The model would change which file later calls use. That is a hidden global switch: one wrong turn and a write goes to the wrong company. It also needs rules for what "selected" means across several clients, and for the audit log and idempotency ledger (which file did a key belong to?).
- It adds a tool and state to every conversation for a need most people (one company) do not have.
- Verdict: no.

### B. One server entry per file (what to do now)
```json
"quickbooks-acme":  { "command": "npx", "args": ["-y", "@devwithmj/quickbooks-mcp"], "env": { "QB_COMPANY_FILE": "C:\Books\Acme.qbw" } },
"quickbooks-globex": { "command": "npx", "args": ["-y", "@devwithmj/quickbooks-mcp"], "env": { "QB_COMPANY_FILE": "C:\Books\Globex.qbw" } }
```
- The file is chosen by configuration, visible to the user, and tied to the server's name in the AI app, so the model cannot mix them up silently.
- Each entry has its own switches, so one file can be read-only and another writable.
- It needed one fix, made with this note: calls used to take turns per company file, so two servers for different files could hit QuickBooks at the same moment. The lock is now one per computer (QuickBooks has a single file open anyway), so they queue instead of colliding. The per-computer name can be overridden with `LockName` for tests.
- Limits to document, not fix: switching files fails while a person has a different file open in the QuickBooks window, and each switch makes QuickBooks close one file and open another, which is slow on large files.
- Verdict: yes, documented in the guide.

### C. Tools that take a `companyFile` argument on every call
Same hidden-switch risk as A, and it multiplies the surface the write gate has to protect. No.

## Revisit when
There is demand from accountants or bookkeepers who manage many clients' files in one conversation. Then it would need: an explicit allowlist of files, the file name echoed in every result and in every write preview, and confirmation (elicitation) when the file changes.
