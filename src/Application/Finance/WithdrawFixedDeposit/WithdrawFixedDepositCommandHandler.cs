using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.WithdrawFixedDeposit;

internal sealed class WithdrawFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<WithdrawFixedDepositCommand>
{
    public async Task<Result> Handle(WithdrawFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsAdmin && !userContext.IsSuperAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.IsSuperAdmin && fixedDeposit.GroupId != userContext.GroupId)
            return Result.Failure(FixedDepositErrors.NotInGroup);

        var result = fixedDeposit.Withdraw(command.InterestEarned, command.WithdrawnAt, userContext.UserId);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
