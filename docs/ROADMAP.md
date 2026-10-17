# Roadmap to 1.0

A snapshot of the plan; the issue tracker is the source of truth (the tracking issue is #53). 61 of 67 planned items were closed when this was written.

Pick anything labelled `good first issue` or `help wanted`. Items still open need something code alone cannot give: real QuickBooks installs, a maintainer's account or repository setting, or a recording.

### Phase 0 - Foundation
- [x] #2 chore: CI for build, test and lint on Windows
- [x] #3 chore: contribution standards (CONTRIBUTING, templates, CODE_OF_CONDUCT, SECURITY)
- [x] #4 chore: solution layout and coding conventions
- [x] #5 docs: legal note on Intuit SDK and trademarks
- [x] #54 docs: project scope and explicit non-goals
- [x] #55 chore: dependency and code scanning (Dependabot, CodeQL, license audit, SBOM)
- [ ] #56 docs: governance, maintainers and GitHub Discussions

### Phase 1 - QuickBooks connectivity
- [x] #6 feat(core): qbXML request/response builder and parser
- [x] #7 feat(core): COM session wrapper over QBXMLRP2.RequestProcessor
- [x] #8 feat(core): cross-process locking, retries and timeouts
- [x] #9 feat(cli): --authorize flow and --doctor command
- [x] #10 feat(core): configuration model (env, args, file)
- [x] #11 test: QuickBooks simulator and recorded fixtures
- [x] #57 feat(core): detect elevation mismatch between QuickBooks and the server
- [x] #58 feat(core): logging and diagnostics bundle (no telemetry)
- [x] #59 feat(core): qbXML version fallback and per-version field handling

### Phase 2 - Read-only MCP
- [x] #12 feat(mcp): stdio MCP server skeleton with official .NET SDK
- [x] #13 feat(mcp): generic qb_query tool with filters and paging
- [x] #14 feat(mcp): query tools for company, preferences and setup lists
- [x] #15 feat(mcp): query tools for customers, vendors, employees, other names, leads
- [x] #16 feat(mcp): query tools for items
- [x] #17 feat(mcp): query tools for sales transactions
- [x] #18 feat(mcp): query tools for purchase transactions and banking
- [x] #19 feat(mcp): resources and prompts (common workflows)
- [x] #20 feat(mcp): result shaping, truncation and PII redaction options
- [x] #60 feat(core): multi-currency and company-file differences
- [ ] #61 perf: large company files, paging defaults and list caching

### Phase 3 - Reports
- [x] #21 feat(reports): aging reports (AR and AP)
- [x] #22 feat(reports): general summary and detail reports
- [x] #23 feat(reports): job, time, budget and custom reports
- [x] #24 feat(reports): tabular output friendly to LLMs and charts

### Phase 4 - Safe writes
- [x] #25 feat(security): write gate, read-only default and per-entity allowlist
- [x] #26 feat(security): dry-run mode, confirmation tokens and audit log
- [x] #27 feat(mcp): create/modify lists (customers, vendors, items, accounts, classes, etc.)
- [x] #28 feat(mcp): create/modify sales transactions
- [x] #29 feat(mcp): create/modify purchase transactions and banking
- [x] #30 feat(mcp): void and delete transactions and list items
- [x] #31 feat(mcp): batch operations and idempotency
- [x] #32 test: write-path safety test suite
- [x] #62 feat(mcp): elicitation for write confirmation, progress and cancellation

### Phase 5 - Advanced entities
- [x] #33 feat(mcp): inventory tools (adjustments, transfers, assemblies, sites)
- [x] #34 feat(mcp): time tracking, mileage and to-dos
- [x] #35 feat(mcp): payroll lists and workers comp (redacted by default)
- [x] #36 feat(mcp): custom fields (DataExt) and sales tax
- [x] #37 feat(mcp): change detection (ListDeleted, TxnDeleted, modified-since sync)
- [x] #38 spike: QuickBooks events and Web Connector for remote or headless use
- [x] #39 spike: credit card processing (CreditCardTxnInfo) scope decision
- [x] #63 feat(mcp): open records in the QuickBooks window (ListDisplay, TxnDisplay)
- [x] #64 spike: switching between multiple company files

### Phase 6 - Packaging and distribution
- [x] #40 build: self-contained single-file release for x64 and x86
- [x] #41 feat(packaging): npm launcher package for npx
- [x] #42 feat(packaging): NuGet MCP package for dnx
- [x] #43 feat(packaging): desktop extension bundle (.mcpb)
- [x] #44 feat(packaging): publish to the official MCP Registry
- [ ] #45 chore: submit to MCP directories and marketplaces
- [x] #46 chore: release automation and versioning

### Phase 7 - Release 1.0
- [x] #47 docs: user guide with screenshots (install, authorize, connect clients)
- [x] #48 docs: tool reference (generated from tool schemas)
- [x] #49 docs: security and privacy model
- [x] #50 security: prompt-injection and data-exfiltration review
- [ ] #51 test: end-to-end validation on multiple QuickBooks versions
- [ ] #52 release: 1.0 checklist
- [x] #65 docs: demo company file and prompt cookbook
- [x] #66 docs: support policy, upgrades and uninstall
- [ ] #67 docs: project site and demo video
- [x] #68 docs: README comparison with the QuickBooks Online MCP server
- [x] #69 docs: tool-naming and tool-count guidelines

## What 1.0 means
See [RELEASE-1.0.md](RELEASE-1.0.md) for the checklist.
