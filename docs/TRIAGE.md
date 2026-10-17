# Triage guide

## Labels
Defined in `.github/labels.yml` and synced by a workflow, so edit that file to change them.

| Group | Labels | Meaning |
|---|---|---|
| Type | `bug`, `enhancement`, `docs`, `security`, `question` | What kind of report it is. Security reports should not be public; point them to SECURITY.md. |
| Area | `area:core`, `area:tools`, `area:reports`, `area:writes`, `area:packaging`, `area:docs` | Where the work is. |
| Status | `needs-info`, `confirmed`, `blocked`, `wontfix`, `duplicate` | Where it is in the process. |
| Priority | `p1`, `p2`, `p3` | p1: data safety or the server cannot start; p2: a feature is broken; p3: polish. |
| Help | `good first issue`, `help wanted` | Suitable for newcomers / maintainers would welcome a PR. |
| Environment | `qb:pro`, `qb:premier`, `qb:enterprise`, `qb:2022-or-older` | Which QuickBooks the report is about. |

## Flow
1. **First look (within about a week):** thank the reporter, apply a type and an area. Bugs missing `--doctor` output, the QuickBooks edition and year, and the server version get `needs-info`.
2. **Reproduce:** with the sample company file if possible. Never ask anyone to post company data; ask for the redacted request or the error text instead.
3. **Decide:** `confirmed` + priority, or close with a reason (`wontfix` when it is outside [SCOPE.md](SCOPE.md), `duplicate` with a link).
4. **Safety first:** anything that can change data wrongly, bypass the write gate, leak redacted data or accept card data is `p1` and `security` if it is exploitable; move the discussion to a private advisory.
5. **Close the loop:** link the fixing PR with `Closes #n`, and mention the release it ships in.

## Stale items
`needs-info` with no answer for 30 days gets one reminder and is closed 14 days later; it can be reopened.
