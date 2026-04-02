using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Create;

internal sealed class CreateMemberCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IUserContext userContext)
    : ICommandHandler<CreateMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMemberCommand command, CancellationToken cancellationToken = default)
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

        if (!group.IsActive)
        {
            return Result.Failure<Guid>(GroupErrors.NotActive);
        }

        bool emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var member = User.Create(
            command.Email,
            passwordHash,
            command.FirstName,
            command.LastName,
            UserRole.Member,
            command.GroupId);

        dbContext.Users.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(member.Id);
    }
}
