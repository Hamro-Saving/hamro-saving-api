using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Abstractions.Settings;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HamroSavings.Application.Members.Create;

internal sealed class CreateMemberCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IEmailService emailService,
    IOptions<FrontendSettings> frontendSettings)
    : ICommandHandler<CreateMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMemberCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
            return Result.Failure<Guid>(UserErrors.NotInGroup);

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
            return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId));

        if (!group.IsActive)
            return Result.Failure<Guid>(GroupErrors.NotActive);

        if (command.MembershipType == MembershipType.Member)
        {
            bool emailExists = await dbContext.Members
                .AnyAsync(m => m.Email == command.Email!.ToLowerInvariant()
                            && m.GroupId == command.GroupId
                            && m.MembershipType == MembershipType.Member, cancellationToken);

            if (emailExists)
                return Result.Failure<Guid>(MemberErrors.EmailNotUnique);

            var member = Member.Create(
                command.FirstName,
                command.LastName!,
                command.Email!,
                command.PhoneNumber,
                command.GroupId);

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);

            var user = User.CreateForMember(member.Email!, member.Id);
            var inviteToken = user.GenerateInviteToken();
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            var signupLink = $"{frontendSettings.Value.Url}/signup?token={inviteToken}";
            await emailService.SendMemberInviteAsync(member.Email!, member.FullName, signupLink, cancellationToken);

            return Result.Success(member.Id);
        }
        else
        {
            var member = Member.CreateNonMember(
                command.FirstName,
                command.Email,
                command.PhoneNumber,
                command.Address,
                command.GroupId);

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(member.Id);
        }
    }
}
