# Governance

## Maintainers
| Name | GitHub | Role |
|---|---|---|
| Majid Allahverdi | [@devwithmj](https://github.com/devwithmj) | Lead maintainer |

Maintainers review and merge pull requests, cut releases, handle security reports and moderate the community spaces.

## How decisions are made
- **Everyday changes** (fixes, tools within scope, docs): a pull request with a maintainer's approval. Anyone can review.
- **Larger changes** (a new capability, anything touching the write path, security, the public tool names, packaging): open an issue first and agree on the approach there. Changes that widen what the server can do to a company file need an explicit written reason in the issue.
- **Scope**: the lines in [SCOPE.md](docs/SCOPE.md) and the safety defaults in the [security model](docs/SECURITY-MODEL.md) change only through an issue that argues the change and gets a maintainer's decision. Defaults stay safe: read-only, redaction on, payroll off, no card data.
- **Disagreements**: discuss on the issue; if there is no agreement the lead maintainer decides and writes down why.
- **Breaking changes**: before 1.0 they are allowed but called out in the release notes (`feat!:` or a `BREAKING CHANGE:` footer). After 1.0, tool names and parameters change only in a major version.

## Becoming a maintainer
Sustained, good contributions: several merged pull requests across different areas, useful reviews, and care about the safety model. A current maintainer can nominate you in an issue or privately; the existing maintainers agree, and you are added with the same rights. Maintainers who are inactive for a year are moved to emeritus with thanks, and can return on request.

## Conduct and security
Everyone follows the [Code of Conduct](CODE_OF_CONDUCT.md). Vulnerabilities go through [SECURITY.md](SECURITY.md), never public issues.

## Where to talk
- **Bugs and features:** issues, using the templates.
- **Questions, ideas and show-and-tell:** GitHub Discussions (Q&A, Ideas, Show and tell).
- **Triage:** see [docs/TRIAGE.md](docs/TRIAGE.md).

## Money and trademarks
This is an independent, volunteer project and is not affiliated with Intuit; see [NOTICE.md](NOTICE.md). Contributions are under the repository's MIT license.
