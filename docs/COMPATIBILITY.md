# Compatibility

This server has been built and tested against recorded responses and Intuit's qbXML 17.0 schema, and run against **one real QuickBooks Desktop: Enterprise Accountant 24.0** with Intuit's sample company file. Every other cell below is *untested*. Results are filled in by following the [tester guide](TESTER-GUIDE.md) (or the longer steps in the next section) and sending a pull request or a compatibility report.

## How to test a combination

On a Windows machine with the edition and year you want to check, and Intuit's **sample company file** (never a real one):

1. Install the build under test (any install route in the [guide](GUIDE.md)).
2. Open the sample file as Admin in the mode you want to test and run `--authorize`.
3. Run `--doctor` and keep its output.
4. From the repository: `set QB_COMPANY_FILE=<path to sample .qbw>` then `dotnet test tests/QuickBooks.Mcp.Core.Tests`. The skipped "integration" tests then run: host and company info, preferences, the common lists, the common transactions, and four reports.
5. Repeat with QuickBooks closed (needs "allow access even if QuickBooks is not running") and with the file in multi-user mode.
6. Add a row to the table: edition, year, qbXML version `--doctor` printed, bitness, and pass or the failing test and error.

## Matrix

Legend: **pass**, **partial** (say what failed), **fail**, *untested*.

| Edition | Year | qbXML | Bitness | Single-user, QB open | Multi-user, QB open | QB closed | Notes |
|---|---|---|---|---|---|---|---|
| Pro | 2022 | | | *untested* | *untested* | *untested* | |
| Pro | 2023 | | | *untested* | *untested* | *untested* | |
| Pro | 2024 | 17.0 | 64-bit | pass | pass | *untested* | Sample company file (Larry's Landscaping), Windows 11. Every read tool, all four reports and the write round-trip (create invoice → idempotent replay → void behind the confirmation token) pass in single-user and multi-user mode; bogus and reused confirmation tokens are refused, and a wrong write field name is refused by the schema before anything is sent. Multi-user needs a QuickBooks user (create one when the login dialog appears). Not run with QuickBooks closed. |
| Premier | 2022 | | | *untested* | *untested* | *untested* | |
| Premier | 2023 | | | *untested* | *untested* | *untested* | |
| Premier | 2024 | | | *untested* | *untested* | *untested* | |
| Enterprise | 22 | | | *untested* | *untested* | *untested* | |
| Enterprise | 23 | | | *untested* | *untested* | *untested* | |
| Enterprise | 24 | | | *untested* | *untested* | *untested* | |
| Enterprise Accountant | 24.0 | 17.0 | 64-bit | pass | pass | partial | Sample company file, Windows 11. Every read tool ran in multi-user mode; the 14 requests QuickBooks refuses on this edition (leads, sales tax returns, payroll period, multi-currency, inventory sites, bar codes) are expected refusals. Merging records needs single-user mode (multi-user returns 3500). With QuickBooks closed, `--doctor` connected and was authorized; the full suite was not run closed. |

## Company options

Some results depend on how the company file is set up, not on the edition. These are tracked separately.

| Option | Status | Notes |
|---|---|---|
| Multi-currency | *untested live* | The server returns the home currency and a plain note whenever a result holds foreign-currency records, and unit tests cover it. The one real file used so far does not have multi-currency on, so QuickBooks refuses currency requests there (code 3250). To test it, follow the multi-currency section of the [tester guide](TESTER-GUIDE.md#multi-currency-test-optional) and send the result. |

Older years and newer ones follow the same procedure. Add rows rather than assuming.

## What is known without a real QuickBooks
- The server asks QuickBooks which qbXML versions it supports and uses the highest one up to 17.0.
- Every request the server builds is checked against the qbXML 17.0 schema in the tests, so element names and order are right for that version. A request using something added after the installed QuickBooks's version will be refused by QuickBooks with its own error; per-version handling is tracked in #59.
- Features that need QuickBooks Enterprise (inventory sites, per-site stock, advanced inventory, some reports) return QuickBooks' error on Pro and Premier.
- Bitness: older QuickBooks registers only a 32-bit request processor (use the `win-x86` build); newer registers a 64-bit one too. `--doctor` shows what the machine has.
- Multi-currency, budgets and other company options change what a file contains; the sample file does not cover them (#60).

## qbXML versions
QuickBooks reports the newest qbXML version it understands; the server uses the highest one up to 17.0. Newer features fail early with a clear message instead of a parser error:

- A request that first appeared in a newer version is refused before it is sent (for example inventory sites need 10.0 and leads need 13.0).
- A field in a create or update that is newer than the installed version is refused by name (`CustomerAdd/ClassRef needs 12.0`), and nothing is sent. `describe_write_fields` marks such fields with `12.0+`.
- The tables are generated from Intuit's schema history (`tools/write-schema`): `qbxml-request-versions.json` for requests and the `since` map in `write-schema.json` for fields.
- Reports and the filters on queries use long-standing elements and are not version-checked individually; if one fails on an old QuickBooks, QuickBooks' own message is shown.
