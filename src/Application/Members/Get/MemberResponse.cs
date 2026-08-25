using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Get;

public sealed record MemberResponse(
    Guid Id,
    string? Email,
    string FirstName,
    string? LastName,
    string FullName,
    GroupRole GroupRole,
    Guid GroupId,
    bool IsActive,
    bool HasAccount,
    /// <summary>
    /// What this person has actually put in. Verified deposits only, matching the financial
    /// summary and the ledger — unconfirmed money is not collected money.
    /// </summary>
    decimal TotalDeposits,
    /// <summary>Principal still out with this person across their live loans.</summary>
    decimal OutstandingPrincipal,
    /// <summary>Interest owed on those loans, accrued to today — the same figure the loans list shows.</summary>
    decimal OutstandingInterest,
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAt);
