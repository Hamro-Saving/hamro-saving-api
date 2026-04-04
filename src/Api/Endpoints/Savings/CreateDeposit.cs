using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.CreateDeposit;
using HamroSavings.Domain.Savings;

namespace HamroSavings.Api.Endpoints.Savings;

public sealed class CreateDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("deposits", async (
            CreateDepositRequest request,
            ICommandHandler<CreateDepositCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateDepositCommand(
                request.MemberId,
                request.GroupId,
                request.Amount,
                request.DepositMonth,
                request.DepositYear,
                request.DepositDate,
                request.Type,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/deposits/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Record a deposit");
    }
}

public sealed record CreateDepositRequest(
    Guid MemberId,
    Guid GroupId,
    decimal Amount,
    int DepositMonth,
    int DepositYear,
    DateOnly DepositDate,
    DepositType Type,
    string? Notes);
