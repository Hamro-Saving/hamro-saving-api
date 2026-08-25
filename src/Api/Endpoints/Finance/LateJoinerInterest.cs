using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetLateJoinerInterest;
using HamroSavings.Application.Finance.RecordLateJoinerInterest;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class RecordLateJoinerInterest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("late-joiner-interest", async (
            RecordLateJoinerInterestRequest request,
            ICommandHandler<RecordLateJoinerInterestCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new RecordLateJoinerInterestCommand(
                request.MemberId, request.Amount, request.PaidDate, request.Notes, request.GroupId);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/late-joiner-interest/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Record interest paid by a member who joined late (group admin only)");
    }
}

public sealed class GetLateJoinerInterest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("late-joiner-interest", async (
            Guid? groupId,
            IQueryHandler<GetLateJoinerInterestQuery, List<LateJoinerInterestResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLateJoinerInterestQuery(groupId), ct);
            return result.Match(
                rows => Results.Ok(rows),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Interest paid by late-joining members");
    }
}

public sealed record RecordLateJoinerInterestRequest(
    Guid MemberId,
    decimal Amount,
    DateTime PaidDate,
    string? Notes,
    Guid? GroupId);
