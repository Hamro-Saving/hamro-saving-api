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
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    IOptions<FrontendSettings> frontendSettings)
    : ICommandHandler<CreateMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMemberCommand command, CancellationToken cancellationToken = default)
    {
        // Admins and members act in the group on their token; only a SuperAdmin names one
        var groupResult = userContext.ResolveGroupId(command.GroupId);
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));

        if (!group.IsActive)
            return Result.Failure<Guid>(GroupErrors.NotActive);

        if (command.MembershipType == MembershipType.Member)
        {
            bool emailExists = await dbContext.Members
                .AnyAsync(m => m.Email == command.Email!.ToLowerInvariant()
                            && m.GroupId == groupId
                            && m.MembershipType == MembershipType.Member, cancellationToken);

            if (emailExists)
                return Result.Failure<Guid>(MemberErrors.EmailNotUnique);

            var member = Member.Create(
                command.FirstName,
                command.LastName!,
                command.Email!,
                command.PhoneNumber,
                groupId);

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);

            var user = User.CreateMember(member.Email!, member.Id, passwordHasher.Hash(Guid.NewGuid().ToString()));
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
                groupId);

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(member.Id);
        }
    }
}
