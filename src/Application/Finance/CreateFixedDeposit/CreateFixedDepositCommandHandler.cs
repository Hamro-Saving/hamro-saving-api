using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.CreateFixedDeposit;

internal sealed class CreateFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateFixedDepositCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(UserErrors.NotInGroup);
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId));
        }

        var fixedDeposit = FixedDeposit.Create(
            command.GroupId,
            command.InstitutionName,
            command.Amount,
            command.InterestRate,
            command.StartDate,
            command.MaturityDate,
            command.Notes,
            userContext.UserId);

        dbContext.FixedDeposits.Add(fixedDeposit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(fixedDeposit.Id);
    }
}
