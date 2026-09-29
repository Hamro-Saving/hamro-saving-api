# AGENTS.md

HamroSavings API: the backend for community savings groups. Members make monthly deposits,
borrow from the pool, vote on loans, and the group keeps double-entry books. It is a .NET 10
minimal API over PostgreSQL, built as a Clean Architecture solution with vertical slices.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# with nullable + implicit usings |
| Web | ASP.NET Core minimal APIs, OpenAPI + Scalar UI (dev only) |
| Data | EF Core 10 + Npgsql (PostgreSQL 16), snake_case naming |
| Validation | FluentValidation 12, run by a decorator before each command |
| DI scanning | Scrutor (handlers, validators, and domain event handlers are auto-registered) |
| Auth | JWT bearer, BCrypt password hashing |
| Email | MailKit over SMTP, sent from an in-memory domain event queue |
| Logging | Serilog (console) |
| Tests | xUnit 2.9, `Microsoft.AspNetCore.TestHost` |
| CI/CD | GitHub Actions → Azure App Service `sathi-bachat-api` |

## Project structure

```
src/
  SharedKernel/    Result, Error, Entity, domain event interfaces. Depends on nothing.
  Domain/          Entities, enums, *Errors, *DomainEvent, one folder per aggregate area.
  Application/     Use cases: one folder per command/query (Command, Handler, Validator, Response).
                   Also Ledger posting, Notifications (event → email handlers), Abstractions.
  Infrastructure/  EF DbContext, configurations, migrations, JWT, SMTP, event dispatcher.
  Api/             Program.cs, one IEndpoint class per route, policies, rate limiting.
tests/
  UnitTests/        Domain and Application rules (references Domain + Application).
  IntegrationTests/ Real middleware and Infrastructure internals (references Api).
```

Feature areas, the same in every layer: `Auth`, `Groups`, `Members`, `Savings`, `Loans`,
`Finance` (expenses, fixed deposits, other incoming funds), `Ledger`, `Notifications`.

For layer rules, request flow, and the auth model, see [docs/architecture.md](docs/architecture.md).

## Commands

Run these from the repo root. The solution file is `HamroSavings.slnx`.

```bash
docker compose up -d                        # local Postgres on :5432
dotnet build                                # warnings are errors (Directory.Build.props)
dotnet test                                 # all tests; no database needed
dotnet run --project src/Api                # http://localhost:7000/scalar
dotnet ef migrations add <Name> --project src/Infrastructure --output-dir Database/Migrations
```

Migrations are applied automatically at startup, and the first SuperAdmin is seeded then too.
For config keys, migrations, and deployment, see [docs/development.md](docs/development.md).

## Coding conventions

- **Vertical slice per use case.** `Application/<Area>/<UseCase>/` holds `XCommand` or `XQuery`
  (a `sealed record`), `XCommandHandler` (`internal sealed`, primary constructor), an optional
  `XCommandValidator`, and `XResponse`. The matching `Api/Endpoints/<Area>/X.cs` implements
  `IEndpoint`. See [docs/adding-a-feature.md](docs/adding-a-feature.md).
- **No exceptions for business outcomes.** Return `Result` / `Result<T>` with an `Error` from
  the entity's `static class XErrors` (for example, `Error.Conflict("Deposit.AlreadyVerified", "...")`).
  The endpoint maps it with `result.Match(..., CustomResults.Problem)`.
- **Rich domain entities.** Setters are `private set`, there is a private parameterless ctor
  for EF, creation goes through a static `Create(...)`, and state changes are methods that
  return `Result` and call `Raise(new XDomainEvent(...))`.
- **IDs** are `Guid.CreateVersion7()`. Money is `decimal` with `HasPrecision(18, 2)`. Enums are
  stored as strings (`HasConversion<string>()`) and serialized as strings in JSON.
- **Group scoping comes from the token, never the request.** Use `ResolveWriteGroupId()`,
  `ResolveReadGroupId(...)`, `CanWrite(...)`, and the related helpers in `UserContextExtensions`.
- **Ledger entries go through `LedgerPosting`** extension methods only, posted at *verification*
  and saved in the same `SaveChangesAsync` as the record. See [docs/domain.md](docs/domain.md).
- **Style**: `.editorconfig` applies (Allman braces, 4-space indent, LF, `_camelCase` private fields).
  Use `var` when the type is apparent. File-scoped namespaces `HamroSavings.<Layer>.<Area>...`.
- **Comments explain *why*,** usually the business rule or the risk. Add `<summary>` docs to
  domain types and tests. Match this voice rather than restating code.
- **Tests** are named as sentences (`AVerifiedDepositKeepsItsDate`). The class `<summary>`
  states the rule under test. Test namespaces are `UnitTests.<Area>` / `IntegrationTests.<Area>`.

## Workflow rules

1. **`main` deploys to production.** Every push to `main` builds, tests, and deploys to Azure.
   Work on a branch, and run `dotnet build && dotnet test` before merging.
2. **Zero warnings.** `TreatWarningsAsErrors` is on, so a warning fails CI and blocks deploy.
3. **Never commit secrets.** `src/Api/appsettings.Development.json` is gitignored and holds
   local secrets. Keep `appsettings.json` values blank for secrets.
4. **Schema changes need a migration** in `src/Infrastructure/Database/Migrations`. Check
   with `dotnet ef migrations has-pending-model-changes --project src/Infrastructure`.
   Backfill existing rows in the migration when semantics change.
5. **Verified records are immutable.** Correct unverified records in place. Correct verified
   ones with an opposite entry. Don't add edit or delete paths that bypass this.
6. **Commits use Conventional Commits** (`feat:`, `fix:`, `chore:`, `ci:`, `refactor:`, `test:`).
   Write a lowercase subject that describes the user-visible outcome
   ("feat: let a loan's start date be revised"). The body explains *why*, including the
   rules and trade-offs.
7. **Add tests with behavior changes**: domain rules go in UnitTests; middleware, email
   wording, and DI wiring go in IntegrationTests.
