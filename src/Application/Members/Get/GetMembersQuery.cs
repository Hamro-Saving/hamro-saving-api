using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Get;

/// <summary>
/// <paramref name="Roles"/> selects which kinds of membership to return — e.g. [Member] for the
/// voting and borrowing pool, [Member, Admin] for the full roster, [NonMember] for borrowers.
/// Empty or null means every role.
/// </summary>
public sealed record GetMembersQuery(
    Guid? GroupId = null,
    IReadOnlyList<GroupRole>? Roles = null) : IQuery<List<MemberResponse>>;
