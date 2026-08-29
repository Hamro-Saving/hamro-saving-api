using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.DeleteFixedDeposit;

/// <summary>
/// Removes a placement entered in error. Only while unverified: a verified placement is
/// money the books show sitting with an institution, and that is corrected by an opposite
/// entry rather than by deleting what it refers to.
/// </summary>
internal sealed class DeleteFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteFixedDepositCommand>
{
    public async Task<Result> Handle(DeleteFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        if (fixedDeposit.IsVerified)
            return Result.Failure(FixedDepositErrors.CannotModifyVerified);

        // Belt and braces: an unverified placement should have no entries, and if one somehow
        // exists then deleting the record would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "FixedDeposit" && e.SourceId == fixedDeposit.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(FixedDepositErrors.CannotModifyVerified);

        dbContext.FixedDeposits.Remove(fixedDeposit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
