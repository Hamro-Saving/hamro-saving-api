using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.VerifyFixedDeposit;

internal sealed class VerifyFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyFixedDepositCommand>
{
    public async Task<Result> Handle(VerifyFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        // Checked here, where the money actually leaves the group's hands.
        var inHand = await CashPosition.InHandAsync(dbContext, fixedDeposit.GroupId, cancellationToken);
        var covered = inHand.EnsureCovers(fixedDeposit.Amount);
        if (covered.IsFailure) return covered;

        var result = fixedDeposit.Verify(userContext.UserId);
        if (result.IsFailure) return result;

        dbContext.PostFixedDepositPlaced(fixedDeposit.GroupId, fixedDeposit.Id, fixedDeposit.Amount,
            fixedDeposit.StartDate, $"Fixed deposit placed with {fixedDeposit.InstitutionName}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
