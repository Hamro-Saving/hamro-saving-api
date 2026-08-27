using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HamroSavings.Application.Members.Create;

internal sealed class CreateMemberCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    ILogger<CreateMemberCommandHandler> logger)
    : ICommandHandler<CreateMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMemberCommand command, CancellationToken cancellationToken = default)
    {
        // Provisioning people is administrative, not financial: a group admin adds to their own
        // group, and a SuperAdmin may name one so a new group can be given its first admin.
        var groupResult = userContext.ResolveAdminWriteGroupId(command.GroupId);
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var authResult = userContext.EnsureCanAdminister(groupId);
        if (authResult.IsFailure) return Result.Failure<Guid>(authResult.Error);

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));

        if (!group.IsActive)
            return Result.Failure<Guid>(GroupErrors.NotActive);

        if (!string.IsNullOrEmpty(command.Email))
        {
            bool emailExists = await dbContext.Members
                .AnyAsync(m => m.Email == command.Email.ToLowerInvariant()
                            && m.GroupId == groupId, cancellationToken);

            if (emailExists)
                return Result.Failure<Guid>(MemberErrors.EmailNotUnique);
        }

        var member = command.GroupRole == GroupRole.NonMember
            ? Member.CreateNonMember(
                command.FirstName,
                command.Email,
                command.PhoneNumber,
                command.Address,
                groupId)
            : Member.Create(
                command.FirstName,
                command.LastName!,
                command.Email!,
                command.PhoneNumber,
                groupId,
                command.GroupRole,
                command.Address);

        // Anyone with an email gets a login, non-members included — they sign in to
        // follow their own loan. A borrower recorded without one simply has no account.
        Guid? inviteToken = null;

        if (!string.IsNullOrEmpty(command.Email))
        {
            // One login per email across the platform: someone joining a second group
            // gains another membership row, not another account.
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

            if (user is null)
            {
                user = User.CreateMember(command.Email, passwordHasher.Hash(Guid.NewGuid().ToString()));
                inviteToken = user.GenerateInviteToken();
                dbContext.Users.Add(user);
            }
            else if (!user.IsActive)
            {
                inviteToken = user.GenerateInviteToken();
            }

            member.LinkUser(user.Id);
        }

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        // An already-active account just gains the membership; no invite to send.
        if (inviteToken is not null)
        {
            try
            {
                await emailService.SendMemberInviteAsync(
                    new EmailRecipient(member.Email!, member.FullName), group, inviteToken.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invite email to {Email} for member {MemberId}", member.Email, member.Id);
            }
        }

        return Result.Success(member.Id);
    }
}
