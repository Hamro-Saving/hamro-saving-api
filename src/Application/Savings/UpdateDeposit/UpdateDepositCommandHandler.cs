using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.UpdateDeposit;

internal sealed class UpdateDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateDepositCommand>
{
    public async Task<Result> Handle(UpdateDepositCommand command, CancellationToken cancellationToken = default)
    {
        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == command.DepositId, cancellationToken);

        if (deposit is null)
            return Result.Failure(DepositErrors.NotFound(command.DepositId));

        if (!userContext.IsSuperAdmin && deposit.GroupId != userContext.GroupId)
            return Result.Failure(DepositErrors.NotInGroup);

        // Members can only edit their own deposits; admins can edit any in their group
        if (!userContext.IsSuperAdmin && !userContext.IsAdmin && deposit.MemberId != userContext.UserId)
            return Result.Failure(UserErrors.Unauthorized);

        var result = deposit.Update(command.Amount, command.Notes);
        if (result.IsFailure)
            return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
