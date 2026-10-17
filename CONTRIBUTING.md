# Contributing

## Setup
- Windows, .NET SDK 10, and QuickBooks Desktop (only needed for integration tests).
- `dotnet build` and `dotnet test` (full steps: [docs/BUILDING.md](docs/BUILDING.md)). Integration tests run only when `QB_COMPANY_FILE` is set.

## Workflow
1. Branch from `main` (`git checkout --no-track -b type/short-name`). Never commit to `main`.
2. Use [Conventional Commits](https://www.conventionalcommits.org): `type(scope): summary`.
3. Open a pull request that references the issue (`Closes #N`). CI must pass.

## Never commit
- QuickBooks company files (`*.qbw`, `*.nd`, `*.tlg`, `*.ds`, ...).
- Intuit SDK files (`Source/`, `Interop.QBFC*.dll`) or any other Intuit binary.
- Secrets, tokens or `.env` files.

## Code
Keep it simple; no speculative abstractions. One tool class per QuickBooks category. XML doc comments on public members.

## Scope
Before proposing a feature, read [docs/SCOPE.md](docs/SCOPE.md): QuickBooks Online, Mac, card processing and running as a Windows service are deliberately out of scope.
