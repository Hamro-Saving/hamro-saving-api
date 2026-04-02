using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Savings.GetDeposits;

public sealed record DepositResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid GroupId,
    decimal Amount,
    int DepositMonth,
    int DepositYear,
    DepositType Type,
    string? Notes,
    bool IsVerified,
    Guid? VerifiedById,
    DateTime? VerifiedAt,
    DateTime CreatedAt);
