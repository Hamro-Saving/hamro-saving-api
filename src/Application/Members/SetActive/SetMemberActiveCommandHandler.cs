using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.SetActive;

/// <summary>
/// Takes someone out of a group, or puts them back.
///
/// This is how a person leaves — not deletion. Their deposits, loans and payments are the
/// group's history and the books are built from them, so the record stays and only their
/// standing changes.
///
/// Deactivating cuts a person off: the membership stops being loaded at sign-in, so the
/// token carries no role, no vote and no access to the group's money, and the login goes with
/// it once there is no other group left to serve. For a non-member — who borrows from the
/// group without a say in it — the same act means the group will not lend to them again, and
/// they are done signing in; what they already owe stays on the books either way.
/// </summary>
internal sealed class SetMemberActiveCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<SetMemberActiveCommand>
{
    public async Task<Result> Handle(SetMemberActiveCommand command, CancellationToken cancellationToken = default)
    {
        // Deliberately unfiltered by IsActive: an inactive member is exactly who is being
        // brought back.
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        if (member.IsActive == command.IsActive)
            return Result.Success();

        if (command.IsActive)
        {
            member.Activate();
        }
        else
        {
            // Refuse to leave a group with nobody able to run it.
            if (member.GroupRole == GroupRole.Admin)
            {
                bool otherAdminExists = await dbContext.Members
                    .AnyAsync(m => m.GroupId == member.GroupId
                                && m.Id != member.Id
                                && m.IsActive
                                && m.GroupRole == GroupRole.Admin, cancellationToken);

                if (!otherAdminExists)
                    return Result.Failure(MemberErrors.LastAdmin);
            }

            member.Deactivate();
        }

        await SetLoginToMatch(member, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// The login is shared across every group this person belongs to, so it only follows the
    /// membership when there is no other one left to serve.
    /// </summary>
    private async Task SetLoginToMatch(Member member, CancellationToken cancellationToken)
    {
        if (member.UserId is null) return;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

        if (user is null) return;

        bool activeElsewhere = await dbContext.Members
            .AnyAsync(m => m.UserId == member.UserId
                        && m.Id != member.Id
                        && m.IsActive, cancellationToken);

        if (member.IsActive)
        {
            // An unaccepted invite is also an inactive login, and it must stay that way:
            // signing up is what activates it, and flipping it here would spend the invite
            // without anyone ever setting a password.
            if (user.InviteToken is null) user.Activate();
        }
        else if (!activeElsewhere)
        {
            user.Deactivate();
        }
    }
}
