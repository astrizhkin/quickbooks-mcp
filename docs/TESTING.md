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
