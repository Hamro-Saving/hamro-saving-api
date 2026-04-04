using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.UpdateDeposit;

namespace HamroSavings.Api.Endpoints.Savings;

public sealed class UpdateDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("deposits/{id:guid}", async (
            Guid id,
            UpdateDepositRequest request,
            ICommandHandler<UpdateDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new UpdateDepositCommand(id, request.Amount, request.Notes), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Update an unverified deposit (owner or Admin/SuperAdmin)");
    }
}

public sealed record UpdateDepositRequest(decimal Amount, string? Notes);
