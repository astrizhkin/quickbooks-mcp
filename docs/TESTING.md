# Testing

```
dotnet test
```

Unit tests need no QuickBooks. Integration tests are skipped unless `QB_COMPANY_FILE` points at a company file
(use Intuit's sample company file, never real data).

## Fixtures
`tests/QuickBooks.Mcp.TestSupport/Fixtures/{Entity}QueryRs.xml` holds sanitized qbXML replies. `FixtureQbClient`
answers a `FooQueryRq` with `FooQueryRs.xml` and records every request it receives.

To add a fixture: capture a reply from the sample company file, remove anything personal, save it under
`Fixtures/` with the response element name as the file name.

## Writing a tool test
Create a `FixtureQbClient`, pass it to the tool class, call the tool, and assert on the result and on
`client.Requests` (the qbXML that was sent).

## Live tests against a real QuickBooks
These need QuickBooks Desktop on the same computer, the app authorized for the file (`--authorize`, see [BUILDING.md](BUILDING.md)), and the file open or reachable. Use Intuit's sample company file or a copy of it.

```
set QB_COMPANY_FILE=C:\path\to\sample.qbw
dotnet test --filter "FullyQualifiedName~Integration|FullyQualifiedName~LiveQuickBooks"
```

- `QuickBooksIntegrationTests` (Core): the client, queries and core reports.
- `LiveQuickBooksTests` (Server): starts the real server in-process and calls every read-only tool through the MCP protocol: every entity with `query_records`, every report type with `get_report`, every typed search and report tool. Each test lists all of its failures together, so one run shows everything that is wrong. Without write switches it also checks that no create, update, delete, void or payroll tool is offered.
- Write lifecycle (creates, changes and deletes one customer): run on a **copy** of the sample file with `set QB_TEST_WRITES=1`.

Not exercised live: `open_in_quickbooks` (changes the screen) and payroll tools (separate opt-in).
