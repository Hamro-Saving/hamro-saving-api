using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetFixedDeposits;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class GetFixedDeposits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("fixed-deposits", async (
            Guid? groupId,
            IQueryHandler<GetFixedDepositsQuery, List<FixedDepositResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetFixedDepositsQuery(groupId), ct);
            return result.Match(
                fds => Results.Ok(fds),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Get fixed deposits");
    }
}
