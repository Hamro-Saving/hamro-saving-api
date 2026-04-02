using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.NonMembers.Get;

public sealed record GetNonMembersQuery(Guid? GroupId = null) : IQuery<List<NonMemberResponse>>;
