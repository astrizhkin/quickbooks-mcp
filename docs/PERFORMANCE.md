# Performance and large company files

## What is slow
Each tool call opens a short QuickBooks session (connect, begin, process, end). On a large file the costs are, in rough order: starting QuickBooks if it is closed (seconds to minutes), the session handshake (about a second), and the query itself, which grows with the number of records QuickBooks has to scan and return.

## What the server does about it
- **Small pages.** Searches return 50 records by default, at most 500, and a page is also cut at about 100,000 characters. `next_cursor` continues where the page stopped. Paging re-runs the query and skips the records already seen (QuickBooks' iterators do not survive separate sessions), so reading *deep* into a huge list costs more per page; narrow the search with filters instead of paging far.
- **Ask for less.** `fields` returns only the columns you need, date and name filters narrow the scan, and `includeLineItems` is off unless asked for.
- **A short cache for lists that rarely change** (chart of accounts, items, classes, terms, payment methods, preferences, company info). Replies are remembered for `QB_CACHE_SECONDS` (default 30; 0 turns it off). Any write empties the cache, and the short lifetime bounds how stale it can get when someone edits QuickBooks directly. Customers, vendors, employees, transactions and reports are never cached.
- **Calls take turns.** One company file is accessed by one request at a time across all processes, so a slow call delays the next; `QB_TIMEOUT_SECONDS` (default 60) bounds the wait.
- **Foreign-currency lookups happen once** per session and only when needed.

## Measuring on your file
Set `QB_LOG_LEVEL=Information` (and `QB_LOG_FILE` if you want a file). Every call logs its request and how long it took, for example `QuickBooks CustomerQueryRq answered in 840 ms`, without any company data. Run the questions you care about and read the numbers; `--diagnostics` includes the latest lines. If a request is slow, narrow it (fields, dates, names) and compare. Results from real large files are welcome in the issue tracker; none are published here yet because the project has not been run against a large real file.

## Slow start-up
If QuickBooks is closed, the first call must start it and open the file, which can exceed an AI app's own tool timeout. Open QuickBooks first for the best experience, or raise `QB_TIMEOUT_SECONDS` and your client's timeout. Progress reporting for long calls is part of the client-confirmation work (#62).
