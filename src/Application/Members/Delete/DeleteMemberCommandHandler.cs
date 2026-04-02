using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Delete;

internal sealed class DeleteMemberCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteMemberCommand>
{
    public async Task<Result> Handle(DeleteMemberCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.MemberId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound(command.MemberId));

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
