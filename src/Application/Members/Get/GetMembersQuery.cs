using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Get;

public sealed record GetMembersQuery(
    Guid? GroupId = null,
    bool IncludeAdmins = false,
    MembershipType? MembershipType = null) : IQuery<List<MemberResponse>>;
