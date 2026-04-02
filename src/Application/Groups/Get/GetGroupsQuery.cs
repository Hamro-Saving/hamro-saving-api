using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Groups.Get;

public sealed record GetGroupsQuery : IQuery<List<GroupResponse>>;
