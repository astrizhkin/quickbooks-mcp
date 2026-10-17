# Tool guidelines

Rules for adding or changing MCP tools. Most are enforced by `ToolGuidelineTests`, so a pull request that breaks one fails. Very long tool lists make models pick the wrong tool, so the list is kept short on purpose.

## Names
- **Verb first, snake_case:** `search_`, `get_`, `create_`, `update_`, `delete_`, `void_`. Reports are `get_<report>`. The few others are `query_` (the generic fallback), `describe_` (helpers) and `open_` (UI).
- **The prefix is the safety class.** `create_`, `update_`, `delete_` and `void_` tools are write tools: they are registered only when that operation is switched on, and they go through the write gate, preview, approval and audit log (`WriteOps` maps the prefix in one place). A tool that changes data must use one of those prefixes; a read tool must not.
- **Read tools say so** (`ReadOnly = true`). Write tools do not.

## Descriptions
- One or two sentences, under about 300 characters, saying what it does and anything the model must not guess. No long prose and no examples in schemas; put examples in `ToolExamples`.
- Every parameter has a short description. Say the format (`yyyy-MM-dd`) and the default.
- Descriptions are advice to a model that anyone might read: never put credentials, internal paths or instructions that override the user in them.

## Fewer, broader tools
- **Do not add a tool per entity.** Extend the existing tool with a `type` or `entity` argument (`search_sales_transactions(type: Invoice|SalesReceipt|...)`, `get_report(reportType)`, `create_list_item(entity)`). Avoid near-duplicates: if a new tool differs from an existing one only by the entity, change the existing one.
- A new tool is justified when its *shape* differs: different inputs, a different result, or a different safety class.
- Search tools take `maxResults` and `cursor` and return `next_cursor`; write tools take `dryRun`; creates take `idempotencyKey`.
- Prefer a generic path plus validation (the entity table, the generated write schema) over hand-written per-entity code.

## Tool count
- Budget: at most **60 tools** with everything enabled and **40** in a default install (`ToolGuidelineTests` checks both). The current count is the first line of [TOOLS.md](TOOLS.md).
- Review the count before each release. If a change needs to raise the budget, say why in the pull request and change the number in the test in the same pull request.

## Adding a tool, checklist
1. Check the table in [TOOLS.md](TOOLS.md): could an existing tool take a new argument instead?
2. Put it in the class for its QuickBooks category (`Customers`, `Reports`, ...), not in a catch-all.
3. Return errors through `ToolErrors` so the model always sees the same shape.
4. Add a fixture and a test (`docs/TESTING.md`); requests are validated against the qbXML schema when it is installed.
5. Add an entry to `ToolExamples`, regenerate the reference (`quickbooks-mcp --tools-reference > docs/TOOLS.md`), and run the tests.
6. If it can change data, read [WRITES.md](WRITES.md) and the [security model](SECURITY-MODEL.md) first, and add a write-safety test.
