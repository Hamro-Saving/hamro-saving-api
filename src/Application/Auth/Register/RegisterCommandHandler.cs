using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.Register;

internal sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        bool emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        if (command.GroupId.HasValue)
        {
            var group = await dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == command.GroupId.Value, cancellationToken);

            if (group is null)
            {
                return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId.Value));
            }

            if (!group.IsActive)
            {
                return Result.Failure<Guid>(GroupErrors.NotActive);
            }
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(
            command.Email,
            passwordHash,
            command.FirstName,
            command.LastName,
            command.Role,
            command.GroupId);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
