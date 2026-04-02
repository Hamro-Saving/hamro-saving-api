using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.Get;

public sealed record GetMembersQuery(Guid? GroupId = null, bool IncludeAdmins = false) : IQuery<List<MemberResponse>>;
