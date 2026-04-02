using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Get;

namespace HamroSavings.Application.Groups.GetById;

public sealed record GetGroupByIdQuery(Guid GroupId) : IQuery<GroupResponse>;
