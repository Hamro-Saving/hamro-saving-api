using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.GetDeposits;

namespace HamroSavings.Api.Endpoints.Savings;

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
