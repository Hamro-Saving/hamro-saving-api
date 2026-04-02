using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Update;

internal sealed class UpdateMemberCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateMemberCommand>
{
    public async Task<Result> Handle(UpdateMemberCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.MemberId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.MemberId));
        }

        user.UpdateProfile(command.FirstName, command.LastName);
        user.ChangeRole(command.Role);

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            bool emailTaken = await dbContext.Users
                .AnyAsync(u => u.Email == command.Email.ToLowerInvariant() && u.Id != command.MemberId, cancellationToken);

            if (emailTaken)
            {
                return Result.Failure(UserErrors.EmailNotUnique);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
