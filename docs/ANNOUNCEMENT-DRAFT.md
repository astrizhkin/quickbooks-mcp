# Announcement draft

(Not published. Edit before using, and only publish once the compatibility table has real results.)

---

**An open-source MCP server for QuickBooks Desktop**

If you keep your books in QuickBooks Desktop (Pro, Premier or Enterprise) on Windows, you can now ask Claude and other AI apps about them: who owes you money, what last quarter's profit and loss looks like, which vendors look like duplicates, what changed since yesterday.

It runs on your own PC next to QuickBooks and talks to it through QuickBooks' own SDK. There is no cloud service, no account and no telemetry.

**Safe by default**
- Read-only until you switch changes on; creating, updating, deleting and voiding are separate switches.
- Deletes and voids ask you, in your AI app, before anything happens; every change is in a local audit log.
- Tax IDs and bank and card numbers are masked, payroll is off, card data is never accepted, and text typed into your records is treated as data rather than instructions.

**Install**
`npx -y @devwithmj/quickbooks-mcp`, `dnx DevWithMJ.QuickBooks.Mcp`, or open the `.mcpb` bundle in Claude Desktop. Allow access once inside QuickBooks, add it to your AI app, and ask.

**Honest status**
This is a first release. It is thoroughly tested against recorded responses and QuickBooks' own request schema, but it has only been run against a limited set of QuickBooks versions so far; the compatibility table lists exactly which. Try it on a sample company first, and tell us what you find.

Using QuickBooks Online? Intuit publishes its own MCP server for that.

Source, guide and issues: https://github.com/devwithmj/quickbooks-mcp (MIT license; not affiliated with Intuit).
