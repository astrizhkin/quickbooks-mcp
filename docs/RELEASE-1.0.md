# 1.0 checklist

1.0 means: the tool names, parameters and safety defaults are stable, and the project has been run against real QuickBooks installs. Status is as of the last update to this file.

## Feature complete per milestones
- [x] Foundation, connectivity, read-only tools, reports, safe writes, advanced entities (see [ROADMAP.md](ROADMAP.md)).
- [x] Packaging pipelines written for npm, NuGet, `.mcpb`, GitHub release and the MCP Registry ([RELEASING.md](RELEASING.md)).
- [ ] The release pipeline has run once end to end from a tag (needs the `NPM_TOKEN` and `NUGET_API_KEY` secrets and a first tag, for example `v0.1.0`).
- [ ] Submissions to directories ([DIRECTORIES.md](DIRECTORIES.md), #45).

## Security review done
- [x] Prompt-injection and exfiltration review, with fixes and tests ([SECURITY-MODEL.md](SECURITY-MODEL.md), #50).
- [x] Write path safety suite (#32), approval by the user where the client supports it (#62).
- [x] Dependency scanning, CodeQL, license check and SBOM configured (#55).
- [ ] An independent look at the write gate and the audit log by someone who did not write them.
- [ ] Code signing of the executables once a certificate exists.

## Docs complete
- [x] User guide, tools reference (generated), security model, scope, writes, reports, performance, support, cookbook, tool guidelines, governance and triage.
- [ ] Authorization screenshots and the demo video (need a QuickBooks install, #47 and #67).

## Compatibility table published
- [ ] [COMPATIBILITY.md](COMPATIBILITY.md) filled in from real runs across editions and years, single-user and multi-user, QuickBooks open and closed (#51). Everything is *untested* until this is done, and this is the main thing standing between the current state and 1.0.
- [ ] Timings on a large real company file ([PERFORMANCE.md](PERFORMANCE.md), #61).

## License review done
- [ ] Read the Intuit SDK license terms against how this project is distributed (see [NOTICE.md](../NOTICE.md)): it calls the installed components and redistributes no Intuit binary, but the generated schema tables (`write-schema.json`, `qbxml-request-versions.json`) derive from the SDK's schema files, which carry Intuit's confidentiality header. Confirm that publishing element names and version numbers for interoperability is acceptable, or replace them with tables written from the public reference.
- [x] Dependency licenses checked in CI (all MIT or Apache-2.0 today).

## Announcement
- [ ] Post: what it is, who it is for (Desktop, not Online), the safety model in three lines, install, honest status of compatibility testing. A draft is in [ANNOUNCEMENT-DRAFT.md](ANNOUNCEMENT-DRAFT.md).

## Before tagging
- Re-run `quickbooks-mcp --tools-reference` and confirm the tool count is within budget ([TOOL-GUIDELINES.md](TOOL-GUIDELINES.md)).
- Decide the names that must not change after 1.0: tool names and parameters, setting names, and the QuickBooks application name `QuickBooks MCP`.
- Tag `v1.0.0`; the pipeline does the rest.
