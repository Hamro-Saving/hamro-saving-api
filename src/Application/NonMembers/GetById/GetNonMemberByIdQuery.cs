using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.NonMembers.Get;

namespace HamroSavings.Application.NonMembers.GetById;

public sealed record GetNonMemberByIdQuery(Guid NonMemberId) : IQuery<NonMemberResponse>;
