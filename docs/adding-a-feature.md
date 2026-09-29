# Adding a feature (vertical slice)

This walkthrough uses "verify an expense" as the example. Copy the nearest existing slice.
`Savings/CreateDeposit` and `Savings/VerifyDeposit` are good references.

## 1. Domain (`src/Domain/<Area>/`)

- Add behavior to the entity as a method that returns `Result`. It guards invariants first,
  mutates second, and raises an event last:

  ```csharp
  public Result Verify(Guid verifiedById)
  {
      if (IsVerified) return Result.Failure(ExpenseErrors.AlreadyVerified);
      IsVerified = true;
      VerifiedById = verifiedById;
      VerifiedAt = DateTime.UtcNow;
      Raise(new ExpenseVerifiedDomainEvent(Id, GroupId));
      return Result.Success();
  }
  ```
- Add errors to `<Entity>Errors` as `static readonly Error` fields, or as methods when they
  need an ID. Codes look like `"Entity.Reason"`. There is one class per entity, and one file
  may hold several (for example, `FinanceErrors.cs` holds `ExpenseErrors`, `FixedDepositErrors`, and so on).
- Add new events as `sealed record ...DomainEvent(Guid ...) : IDomainEvent`.

## 2. Application (`src/Application/<Area>/<UseCase>/`)

```csharp
public sealed record VerifyExpenseCommand(Guid ExpenseId) : ICommand;           // or ICommand<T>

internal sealed class VerifyExpenseCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyExpenseCommand>
{
    public async Task<Result> Handle(VerifyExpenseCommand command, CancellationToken cancellationToken = default)
    {
        // 1. authorize (role + group scope via UserContextExtensions)
        // 2. load, NotFound if missing, NotInGroup if !CanWrite
        // 3. call the domain method, return on failure
        // 4. post ledger entries via LedgerPosting, if money moved
        // 5. await dbContext.SaveChangesAsync(cancellationToken) — exactly once
    }
}
```

- Queries use `IQuery<TResponse>` / `IQueryHandler<,>`. They project into a `sealed record
  XResponse` and scope with `ResolveReadGroupId`. Apply `SeesOnlyOwnRecords` where
  non-members could read.
- Validators (`AbstractValidator<XCommand>`) handle input shape only: ranges, required
  fields, and dates not in the future. Business rules go in the domain.
- You don't need to register anything. Scrutor picks up handlers, validators, and event
  handlers.

## 3. API (`src/Api/Endpoints/<Area>/<UseCase>.cs`)

```csharp
public sealed class VerifyExpense : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("expenses/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyExpenseCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyExpenseCommand(id), ct);
            return result.Match(() => Results.NoContent(), error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify an expense (group admin only)");
    }
}
```

- Put the request body record (`XRequest`) at the bottom of the same file.
- Use plural, lowercase routes. Use sub-resource verbs for state changes
  (`/{id:guid}/verify`, `/withdraw`).
- Return `Created($"/api/v1/...", new { Id = id })` for creates and `NoContent()` for commands
  with no payload.
- One file may map closely related routes (for example, `UpdateFixedDeposit.cs`).

## 4. Persistence (only if the model changed)

- Add or update `Infrastructure/Database/Configurations/XConfiguration.cs`
  (`internal sealed`, `IEntityTypeConfiguration<T>`). Use `HasPrecision(18, 2)` for money,
  `HasConversion<string>()` for enums, and `HasMaxLength` for strings.
- For a new aggregate, add a `DbSet` to **both** `IApplicationDbContext` and `HamroSavingsDbContext`.
- `dotnet ef migrations add <PascalCaseName> --project src/Infrastructure --output-dir Database/Migrations`

## 5. Notifications (optional)

Add `Application/Notifications/XEmailHandler.cs` implementing `IDomainEventHandler<XEvent>`,
a method on `IEmailService`/`EmailService`, and wording in `Infrastructure/Email/EmailText.cs`.
Pin the wording with a test in `IntegrationTests/Notifications`.

## 6. Tests

- For domain and handler rules, add `tests/UnitTests/<Area>/XTests.cs`. Use `FakeUserContext.In(groupId, role)`
  for a caller.
- Name tests as sentences, and give the class a `<summary>` explaining the business rule.
