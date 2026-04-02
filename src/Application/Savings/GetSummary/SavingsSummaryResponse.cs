using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Savings.GetSummary;

public sealed record SavingsSummaryResponse(
    decimal TotalDeposits,
    decimal TotalVerifiedDeposits,
    decimal TotalPendingDeposits,
    Dictionary<string, decimal> ByType,
    List<MemberDepositSummary> ByMember);

public sealed record MemberDepositSummary(
    Guid MemberId,
    string MemberName,
    decimal TotalAmount,
    int DepositCount);
