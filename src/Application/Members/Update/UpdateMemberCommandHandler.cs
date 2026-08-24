using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Update;

internal sealed class UpdateMemberCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateMemberCommand>
{
    public async Task<Result> Handle(UpdateMemberCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        if (!string.IsNullOrEmpty(command.Email))
        {
            bool emailTaken = await dbContext.Members
                .AnyAsync(m => m.Email == command.Email.ToLowerInvariant()
                            && m.GroupId == member.GroupId
                            && m.Id != command.MemberId, cancellationToken);

            if (emailTaken)
                return Result.Failure(MemberErrors.EmailNotUnique);
        }

        member.UpdateProfile(command.FirstName, command.LastName, command.Email, command.PhoneNumber, command.Address);

        if (member.GroupRole.Participates() && !string.IsNullOrEmpty(command.Email))
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);
            user?.UpdateEmail(command.Email);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
