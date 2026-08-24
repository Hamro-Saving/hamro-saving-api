using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Abstractions.Settings;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HamroSavings.Application.Members.ResendInvite;

internal sealed class ResendInviteCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    ILogger<ResendInviteCommandHandler> logger,
    IOptions<FrontendSettings> frontendSettings)
    : ICommandHandler<ResendInviteCommand>
{
    public async Task<Result> Handle(ResendInviteCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        if (string.IsNullOrEmpty(member.Email))
            return Result.Failure(MemberErrors.NoEmailToInvite(member.Id));

        var user = member.UserId is null
            ? null
            : await dbContext.Users.FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

        if (user is null)
        {
            // Anyone with an email is invitable, but rows created before non-members were
            // given logins have none, so the first resend is what creates theirs.
            user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == member.Email.ToLowerInvariant(), cancellationToken);

            if (user is null)
            {
                user = User.CreateMember(member.Email, passwordHasher.Hash(Guid.NewGuid().ToString()));
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            member.LinkUser(user.Id);
        }

        if (user.IsActive)
            return Result.Failure(UserErrors.AlreadyActivated);

        var inviteToken = user.GenerateInviteToken();
        await dbContext.SaveChangesAsync(cancellationToken);

        var signupLink = $"{frontendSettings.Value.Url}/signup?token={inviteToken}";

        try
        {
            await emailService.SendMemberInviteAsync(member.Email!, member.FullName, signupLink, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invite email to {Email} for member {MemberId}", member.Email, member.Id);
            return Result.Failure(MemberErrors.InviteEmailFailed);
        }

        return Result.Success();
    }
}
