using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.ReviseWithdrawal;

internal sealed class ReviseFixedDepositWithdrawalCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<ReviseFixedDepositWithdrawalCommand>
{
    public async Task<Result> Handle(ReviseFixedDepositWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        var result = fixedDeposit.ReviseWithdrawal(command.InterestEarned, command.WithdrawnAt);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
