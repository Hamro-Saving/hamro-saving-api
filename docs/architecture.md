# Architecture

## Layers and dependency rule

```
Api ──► Application ──► Domain ──► SharedKernel
 │           ▲
 └──► Infrastructure
```

- **SharedKernel** has no dependencies. It holds `Result`/`Result<T>`, `Error`/`ErrorType`,
  `Entity` (domain event list), `IDomainEvent`, `IDomainEventHandler<T>`,
  `IDomainEventPublisher`, `IDateTimeProvider`, `PagedResult`, and `ValidationError`.
- **Domain** has pure business types and no EF attributes. Persistence mapping lives in
  `Infrastructure/Database/Configurations`.
- **Application** defines the abstractions it needs (`IApplicationDbContext`, `IUserContext`,
  `ITokenProvider`, `IPasswordHasher`, `IEmailService`, `IEmailSender`) under `Abstractions/`.
  It references EF Core only for `DbSet<T>` and LINQ async operators.
- **Infrastructure** implements those abstractions.
- **Api** is the composition root. It maps HTTP to commands and queries and nothing more.

`InternalsVisibleTo`: Application → `HamroSavings.UnitTests`. Api and Infrastructure →
`HamroSavings.IntegrationTests`.

## Request flow

1. `Program.cs` calls `AddEndpoints(assembly)`, which registers every `IEndpoint`. `MapEndpoints`
   mounts them under the `api/v1` route group.
2. The endpoint lambda binds a `XRequest` record, builds the `XCommand`, and resolves
   `ICommandHandler<XCommand, T>` (or `IQueryHandler<,>`) from DI.
3. `ValidationDecorator` (Scrutor `Decorate`) runs every `IValidator<XCommand>`. Failures
   return `ErrorType.Validation` without calling the handler. Queries are not decorated.
4. The handler loads entities through `IApplicationDbContext`, calls domain methods, posts
   ledger entries if needed, and calls `SaveChangesAsync` once.
5. `HamroSavingsDbContext.SaveChangesAsync` drains domain events from tracked entities
   *after* a successful commit and pushes them onto `DomainEventQueue`.
6. The endpoint returns `result.Match(onSuccess, CustomResults.Problem)`.

### Error → HTTP mapping (`Api/Infrastructure/CustomResults.cs`)

| ErrorType | Status |
|---|---|
| Validation, Problem | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Failure / other | 500 |

`Problem` means the caller broke a business rule, so it maps to 400. Use `Failure` only for
real server faults. Unhandled exceptions go to `GlobalExceptionHandler`.

## Domain events and notifications

- Entities call `Raise(...)`. Events are `sealed record XDomainEvent(...) : IDomainEvent`,
  and they carry IDs only.
- `DomainEventQueue` is an unbounded in-memory `Channel` (singleton). `DomainEventProcessor`
  is a `BackgroundService` that gives each event a fresh DI scope and runs every
  `IDomainEventHandler<T>`. If a handler throws, the error is logged and the other handlers
  still run.
- Events are **not durable**: anything still queued at shutdown is lost. Use them only for
  side effects like email, never for bookkeeping.
- Handlers live in `Application/Notifications/*EmailHandler.cs`. They re-load data by ID,
  pick recipients with `NotificationRecipients`, and call `IEmailService`. Wording and
  layout are in `Infrastructure/Email` (`EmailText.cs`, `EmailLayout.cs`).
- Handlers are found by assembly scan. `NotificationWiringTests` proves they resolve.

## Authentication and authorization

JWT claims (`AppClaims`): `is_super_admin`, `GroupId`, `MemberId`, `group_role`,
`memberships`. A user can belong to several groups, and `SwitchGroup` issues a token for
another group.

There are **two independent axes**:

- **Platform:** SuperAdmin. It does *not* imply any group role.
- **Group:** `GroupRole` = `NonMember` (borrows only), `Member`, or `Admin`.

Endpoint policies (`Policies`, registered in `Program.cs`):

| Policy | Who |
|---|---|
| `SuperAdmin` | platform admins |
| `GroupAdmin` | admin of the active group |
| `GroupMember` | member or admin of the active group |
| `GroupRead` | member, admin, or SuperAdmin (cross-group read) |

Handlers also enforce group scope with `UserContextExtensions`:

- `ResolveWriteGroupId()`: always the token's active group. A SuperAdmin gets no escape here,
  because writing financial data requires real membership.
- `ResolveAdminWriteGroupId(requested)`: a SuperAdmin may name a group. Use it only for
  provisioning people and group settings.
- `ResolveReadGroupId(requested)`: a SuperAdmin may name any group or none. Everyone else is
  pinned to their active group.
- `CanRead` / `CanWrite` / `EnsureCanAdminister` / `SeesOnlyOwnRecords` (non-members see
  only their own loans).

Ignore any `GroupId` a request sends for writes.

## Rate limiting (`Api/Infrastructure/RateLimiting.cs`)

There are three tiers, set in `RateLimiting:*` config with the same defaults in code:

- Global: 300/min.
- `SignIn`: 15 per 5 min.
- `AccountRecovery`: 5 per 15 min. Applies to password reset and invite claim.

`UseRateLimiter()` sits after `UseAuthentication()`, so signed-in callers are partitioned by
user, not by IP. Preflights and health checks are exempt. Apply a named policy with
`.RequireRateLimiting(RateLimitPolicies.X)`.
