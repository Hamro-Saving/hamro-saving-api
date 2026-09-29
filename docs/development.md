# Development

## Prerequisites

- .NET SDK 10 (`dotnet --version` should print 10.x)
- Docker, for local Postgres
- `dotnet-ef` global tool: `dotnet tool install -g dotnet-ef`

## Local setup

```bash
docker compose up -d        # postgres:16-alpine, db "hamrosavings", port 5432
```

Create `src/Api/appsettings.Development.json`. It is gitignored, so never commit it. It
needs these keys:

| Key | Notes |
|---|---|
| `ConnectionStrings:HamroSavingsDb` | Npgsql connection string, matching `docker-compose.yml` credentials |
| `Jwt:Secret` | ≥ 32 bytes (HMAC-SHA256) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationInMinutes` | defaults in `appsettings.json` |
| `SuperAdmin:Email`, `SuperAdmin:Password` | **required**. If they are blank, startup throws |
| `Frontend:Url` | used for email links and prod CORS (default `http://localhost:5173`) |
| `Email:SmtpHost`, `SmtpPort`, `Username`, `Password`, `FromAddress` | SMTP for notifications |
| `RateLimiting:*` | optional. Set `Enabled: false` to turn it off locally |

In production these come from App Service configuration / environment variables.

## Run

```bash
dotnet run --project src/Api                      # http profile → http://localhost:7000
dotnet run --project src/Api --launch-profile https   # https://localhost:7001
```

- Scalar API explorer: `/scalar`. OpenAPI JSON: `/openapi/v1.json`. Both are dev only.
- Health checks: `/health`, `/alive`.
- Dev CORS allows `localhost` and ngrok hosts. Prod CORS allows only `Frontend:Url`.
- On startup, the app runs `Database.Migrate()` and seeds the first SuperAdmin if none exists.
  This assumes a single instance. If the app is scaled out, move migrations to a deploy step.

## Tests

```bash
dotnet test                                              # everything
dotnet test tests/UnitTests                              # one project
dotnet test --filter "FullyQualifiedName~Loans"          # one area
dotnet test --filter "FullyQualifiedName~LoanVotingTests.<Method>"
```

- Neither project needs a database. UnitTests work against domain objects and Application
  internals. IntegrationTests use `TestServer` for middleware and build a DI container for
  wiring and email rendering.
- `tests/IntegrationTests/UnitTest1.cs` is an empty scaffold placeholder.

## Migrations

Run these from the repo root. The design-time factory (`HamroSavingsDbContextFactory`) reads
`src/Api/appsettings*.json`, so Infrastructure is both the project and the startup project.

```bash
dotnet ef migrations add AddSomething --project src/Infrastructure --output-dir Database/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure
dotnet ef migrations remove --project src/Infrastructure       # undo last, if not yet deployed
dotnet ef database update --project src/Infrastructure         # optional; startup also migrates
```

Don't use `--startup-project src/Api`. The Api project doesn't reference
`Microsoft.EntityFrameworkCore.Design`.

Migration history was squashed once into `InitialCreate`. Don't squash again after deploys,
because production has applied the existing migrations.

## CI/CD

`.github/workflows/main_sathi-bachat-api.yml` runs on every push to `main` and on manual
dispatch:

1. `dotnet build -c Release` (fails on any warning)
2. `dotnet test -c Release --no-build`
3. `dotnet publish src/Api` → deploy to Azure App Service `sathi-bachat-api` (Production slot)

There is no staging slot, so a green push to `main` is live.
