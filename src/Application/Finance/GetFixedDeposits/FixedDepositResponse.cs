using HamroSavings.Domain.Finance;

namespace HamroSavings.Application.Finance.GetFixedDeposits;

public sealed record FixedDepositResponse(
    Guid Id,
    Guid GroupId,
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    decimal ExpectedMaturityAmount,
    DateTime StartDate,
    DateTime MaturityDate,
    FixedDepositStatus Status,
    string? Notes,
    bool IsVerified,
    DateTime? VerifiedAt,
    // Set once the deposit has been withdrawn
    decimal? InterestEarned,
    DateTime? WithdrawnAt,
    bool IsWithdrawalVerified,
    DateTime? WithdrawalVerifiedAt,
    DateTime CreatedAt);
