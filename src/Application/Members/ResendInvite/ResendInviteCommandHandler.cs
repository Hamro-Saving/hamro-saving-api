using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Abstractions.Settings;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HamroSavings.Application.Members.ResendInvite;

internal sealed class ResendInviteCommandHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService,
    IOptions<FrontendSettings> frontendSettings)
    : ICommandHandler<ResendInviteCommand>
{
    public async Task<Result> Handle(ResendInviteCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.MemberId == command.MemberId, cancellationToken);

        if (user is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        if (user.IsActive)
            return Result.Failure(UserErrors.AlreadyActivated);

        var inviteToken = user.GenerateInviteToken();
        await dbContext.SaveChangesAsync(cancellationToken);

        var signupLink = $"{frontendSettings.Value.Url}/signup?token={inviteToken}";
        await emailService.SendMemberInviteAsync(member.Email!, member.FullName, signupLink, cancellationToken);

        return Result.Success();
    }
}
