using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.ListPayments;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class ListLoanPayments : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loan-payments", async (
            Guid? groupId,
            Guid? borrowerId,
            bool? isVerified,
            IQueryHandler<ListLoanPaymentsQuery, List<LoanPaymentListItemResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ListLoanPaymentsQuery(groupId, borrowerId, isVerified), ct);
            return result.Match(
                payments => Results.Ok(payments),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Get loan payments across a group's loans, with optional filters");
    }
}
