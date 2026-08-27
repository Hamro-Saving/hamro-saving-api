using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.VerifyFixedDepositWithdrawal;

/// <summary>
/// No cash rule applies — the group is receiving, not committing — but the interest figure is
/// whatever someone typed in, and it becomes the group's income.
/// </summary>
internal sealed class VerifyFixedDepositWithdrawalCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyFixedDepositWithdrawalCommand>
{
    public async Task<Result> Handle(VerifyFixedDepositWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        var result = fixedDeposit.VerifyWithdrawal(userContext.UserId);
        if (result.IsFailure) return result;

        var occurredAt = fixedDeposit.WithdrawnAt ?? DateTime.UtcNow;

        dbContext.PostFixedDepositWithdrawal(fixedDeposit.GroupId, fixedDeposit.Id, fixedDeposit.Amount,
            occurredAt, $"Fixed deposit withdrawn from {fixedDeposit.InstitutionName}");

        dbContext.PostFixedDepositInterest(fixedDeposit.GroupId, fixedDeposit.Id, fixedDeposit.InterestEarned ?? 0,
            occurredAt, $"Fixed deposit interest from {fixedDeposit.InstitutionName}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
