using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.CreateFixedDeposit;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class CreateFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("fixed-deposits", async (
            CreateFixedDepositRequest request,
            ICommandHandler<CreateFixedDepositCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateFixedDepositCommand(
                request.GroupId,
                request.InstitutionName,
                request.Amount,
                request.InterestRate,
                request.StartDate,
                request.MaturityDate,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/fixed-deposits/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Create a fixed deposit record");
    }
}

public sealed record CreateFixedDepositRequest(
    Guid GroupId,
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    DateTime StartDate,
    DateTime MaturityDate,
    string? Notes);
