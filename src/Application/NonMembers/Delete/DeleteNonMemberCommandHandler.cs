using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.NonMembers.Delete;

internal sealed class DeleteNonMemberCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteNonMemberCommand>
{
    public async Task<Result> Handle(DeleteNonMemberCommand command, CancellationToken cancellationToken = default)
    {
        var nonMember = await dbContext.NonMembers
            .FirstOrDefaultAsync(n => n.Id == command.NonMemberId, cancellationToken);

        if (nonMember is null)
            return Result.Failure(NonMemberErrors.NotFound(command.NonMemberId));

        dbContext.NonMembers.Remove(nonMember);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
