using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.VerifyDeposit;

internal sealed class VerifyDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyDepositCommand>
{
    public async Task<Result> Handle(VerifyDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && !userContext.IsAdmin)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == command.DepositId, cancellationToken);

        if (deposit is null)
        {
            return Result.Failure(DepositErrors.NotFound(command.DepositId));
        }

        if (!userContext.IsSuperAdmin && deposit.GroupId != command.GroupId)
        {
            return Result.Failure(DepositErrors.NotInGroup);
        }

        var result = deposit.Verify(userContext.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
