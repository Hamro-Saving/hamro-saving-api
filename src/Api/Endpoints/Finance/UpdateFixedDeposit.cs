using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.CancelWithdrawal;
using HamroSavings.Application.Finance.DeleteFixedDeposit;
using HamroSavings.Application.Finance.ReviseWithdrawal;
using HamroSavings.Application.Finance.UpdateFixedDeposit;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class UpdateFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("fixed-deposits/{id:guid}", async (
            Guid id,
            UpdateFixedDepositRequest request,
            ICommandHandler<UpdateFixedDepositCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateFixedDepositCommand(
                id,
                request.InstitutionName,
                request.Amount,
                request.InterestRate,
                request.StartDate,
                request.MaturityDate,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Correct an unverified fixed deposit placement (group admin only)");
    }
}

public sealed class DeleteFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("fixed-deposits/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteFixedDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteFixedDepositCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Delete an unverified fixed deposit placement (group admin only)");
    }
}

/// <summary>
/// Restating a withdrawal already recorded, kept apart from `withdraw` so that withdrawing
/// stays a once-only act and a second attempt at it is still refused.
/// </summary>
public sealed class ReviseFixedDepositWithdrawal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("fixed-deposits/{id:guid}/revise-withdrawal", async (
            Guid id,
            ReviseWithdrawalRequest request,
            ICommandHandler<ReviseFixedDepositWithdrawalCommand> handler,
            CancellationToken ct) =>
        {
            var command = new ReviseFixedDepositWithdrawalCommand(id, request.InterestEarned, request.WithdrawnAt);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Correct an unverified withdrawal's interest or date (group admin only)");
    }
}

public sealed class CancelFixedDepositWithdrawal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("fixed-deposits/{id:guid}/withdraw", async (
            Guid id,
            ICommandHandler<CancelFixedDepositWithdrawalCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CancelFixedDepositWithdrawalCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Take back an unverified withdrawal, leaving the deposit placed (group admin only)");
    }
}

public sealed record UpdateFixedDepositRequest(
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    DateTime StartDate,
    DateTime MaturityDate,
    string? Notes);

public sealed record ReviseWithdrawalRequest(
    decimal InterestEarned,
    DateTime WithdrawnAt);
