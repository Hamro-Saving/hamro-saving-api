using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>
/// Composes so each call site reads as the rule it applies.
///
/// Non-members are excluded throughout: they borrow from the group without a say in it or a
/// right to read its books. <see cref="IncludingBorrower"/> is the one exception, and only
/// ever about their own loan.
/// </summary>
internal static class NotificationRecipients
{
    /// <summary>Mirrors the eligible-voter set — an admin hears everything a member hears.</summary>
    public static IQueryable<Member> Participants(IQueryable<Member> members, Guid groupId) =>
        members.Where(m =>
            m.GroupId == groupId &&
            m.IsActive &&
            m.GroupRole != GroupRole.NonMember &&
            m.Email != null &&
            m.Email != "");

    /// <summary>The only people who can verify an entry.</summary>
    public static IQueryable<Member> AdminsOnly(this IQueryable<Member> members) =>
        members.Where(m => m.GroupRole == GroupRole.Admin);

    /// <summary>Drops the member an event is about — they are its subject, not its audience.</summary>
    public static IQueryable<Member> ExceptMember(this IQueryable<Member> members, Guid? memberId) =>
        memberId is null ? members : members.Where(m => m.Id != memberId);

    /// <summary>
    /// Drops whoever just did it, by <em>user</em> id — that is what the domain records for a
    /// verifier.
    ///
    /// The null check is not redundant: a member with no login cannot be that person, and
    /// comparing their null user id in SQL would drop them from the group's mail.
    /// </summary>
    public static IQueryable<Member> ExceptUser(this IQueryable<Member> members, Guid? userId) =>
        userId is null ? members : members.Where(m => m.UserId == null || m.UserId != userId);

    /// <summary>
    /// Projects after the query runs: a member's full name is assembled in C# from two
    /// columns and cannot be translated to SQL.
    /// </summary>
    public static async Task<List<EmailRecipient>> ToRecipientsAsync(
        this IQueryable<Member> members,
        CancellationToken ct)
    {
        var rows = await members.ToListAsync(ct);
        return [.. rows.Select(m => new EmailRecipient(m.Email!, m.FullName))];
    }

    /// <summary>
    /// A non-member borrower is outside the audience but holds a login to follow their own
    /// loan, so they hear what the group hears about it. A member borrower is already in the
    /// list and is not added twice.
    /// </summary>
    /// <param name="exceptUserId">
    /// Whoever the audience just excluded, so an admin who verified a payment on their own
    /// loan is not added back.
    /// </param>
    public static List<EmailRecipient> IncludingBorrower(
        this List<EmailRecipient> recipients,
        Member borrower,
        Guid? exceptUserId = null)
    {
        if (string.IsNullOrEmpty(borrower.Email)) return recipients;

        if (exceptUserId is not null && borrower.UserId == exceptUserId) return recipients;

        return recipients.Any(r => string.Equals(r.Email, borrower.Email, StringComparison.OrdinalIgnoreCase))
            ? recipients
            : [.. recipients, new EmailRecipient(borrower.Email, borrower.FullName)];
    }
}
