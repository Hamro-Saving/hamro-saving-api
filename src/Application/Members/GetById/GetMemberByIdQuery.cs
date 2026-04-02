using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Get;

namespace HamroSavings.Application.Members.GetById;

public sealed record GetMemberByIdQuery(Guid MemberId) : IQuery<MemberResponse>;
