using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.CreateDeposit;
using HamroSavings.Application.Savings.GetDeposits;
using HamroSavings.Application.Savings.GetSummary;
using HamroSavings.Application.Savings.VerifyDeposit;
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

public sealed class GetDeposits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("deposits", async (
            Guid? groupId,
            Guid? memberId,
            int? month,
            int? year,
            bool? isVerified,
            IQueryHandler<GetDepositsQuery, List<DepositResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetDepositsQuery(groupId, memberId, month, year, isVerified), ct);
            return result.Match(
                deposits => Results.Ok(deposits),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Get deposits with optional filters");
    }
}

public sealed class VerifyDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("deposits/{id:guid}/verify", async (
            Guid id,
            VerifyDepositRequest request,
            ICommandHandler<VerifyDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyDepositCommand(id, request.GroupId), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Verify a deposit (Admin/SuperAdmin only)");
    }
}

public sealed class GetSavingsSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("deposits/summary", async (
            Guid? groupId,
            IQueryHandler<GetSavingsSummaryQuery, SavingsSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetSavingsSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Get savings summary");
    }
}

public sealed record CreateDepositRequest(
    Guid MemberId,
    Guid GroupId,
    decimal Amount,
    int DepositMonth,
    int DepositYear,
    DepositType Type,
    string? Notes);

public sealed record VerifyDepositRequest(Guid GroupId);
