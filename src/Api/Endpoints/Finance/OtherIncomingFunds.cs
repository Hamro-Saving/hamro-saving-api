using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetOtherIncomingFunds;
using HamroSavings.Application.Finance.RecordOtherIncomingFund;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class RecordOtherIncomingFund : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("other-incoming-funds", async (
            RecordOtherIncomingFundRequest request,
            ICommandHandler<RecordOtherIncomingFundCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new RecordOtherIncomingFundCommand(
                request.MemberId, request.Amount, request.PaidDate, request.Remarks, request.GroupId);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/other-incoming-funds/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Record interest paid by a member who joined late (group admin only)");
    }
}

public sealed class GetOtherIncomingFunds : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("other-incoming-funds", async (
            Guid? groupId,
            IQueryHandler<GetOtherIncomingFundsQuery, List<OtherIncomingFundResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetOtherIncomingFundsQuery(groupId), ct);
            return result.Match(
                rows => Results.Ok(rows),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Interest paid by late-joining members");
    }
}

public sealed record RecordOtherIncomingFundRequest(
    Guid MemberId,
    decimal Amount,
    DateTime PaidDate,
    string Remarks,
    Guid? GroupId);
