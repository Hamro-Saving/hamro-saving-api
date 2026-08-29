using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.DeleteOtherIncomingFund;
using HamroSavings.Application.Finance.UpdateOtherIncomingFund;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class UpdateOtherIncomingFund : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("other-incoming-funds/{id:guid}", async (
            Guid id,
            UpdateOtherIncomingFundRequest request,
            ICommandHandler<UpdateOtherIncomingFundCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateOtherIncomingFundCommand(id, request.Amount, request.PaidDate, request.Remarks);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Correct an unverified incoming funds record (group admin only)");
    }
}

public sealed class DeleteOtherIncomingFund : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("other-incoming-funds/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteOtherIncomingFundCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteOtherIncomingFundCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Delete an unverified incoming funds record (group admin only)");
    }
}

public sealed record UpdateOtherIncomingFundRequest(
    decimal Amount,
    DateTime PaidDate,
    string Remarks);
