using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.UpdateFixedDeposit;

/// <summary>
/// Corrects a placement before it is verified — the institution, the figure, the rate, the
/// dates. Nothing is checked against the group's cash here: the money has not moved until
/// the placement is verified, and that is where the balance has to cover it.
/// </summary>
internal sealed class UpdateFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateFixedDepositCommand>
{
    public async Task<Result> Handle(UpdateFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        var result = fixedDeposit.Update(
            command.InstitutionName,
            command.Amount,
            command.InterestRate,
            command.StartDate,
            command.MaturityDate,
            command.Notes);

        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
