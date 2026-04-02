using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.NonMembers.Update;

internal sealed class UpdateNonMemberCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateNonMemberCommand>
{
    public async Task<Result> Handle(UpdateNonMemberCommand command, CancellationToken cancellationToken = default)
    {
        var nonMember = await dbContext.NonMembers
            .FirstOrDefaultAsync(n => n.Id == command.NonMemberId, cancellationToken);

        if (nonMember is null)
            return Result.Failure(NonMemberErrors.NotFound(command.NonMemberId));

        nonMember.Update(command.FullName, command.Email, command.Phone, command.Address);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
