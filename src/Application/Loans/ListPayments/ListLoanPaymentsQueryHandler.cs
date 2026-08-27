using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.ListPayments;

internal sealed class ListLoanPaymentsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<ListLoanPaymentsQuery, List<LoanPaymentListItemResponse>>
{
    public async Task<Result<List<LoanPaymentListItemResponse>>> Handle(ListLoanPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<LoanPaymentListItemResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        // A payment has no group of its own — the loan it belongs to is what carries one.
        var paymentsQuery =
            from p in dbContext.LoanPayments
            join l in dbContext.Loans on p.LoanId equals l.Id
            select new { Payment = p, Loan = l };

        if (groupId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.Loan.GroupId == groupId.Value);
        }

        if (query.BorrowerId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.Loan.BorrowerId == query.BorrowerId.Value);
        }

        if (query.IsVerified.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.Payment.IsVerified == query.IsVerified.Value);
        }

        // A non-member follows their own loans and no one else's.
        if (userContext.SeesOnlyOwnRecords())
        {
            var ownMemberId = userContext.ActiveMemberId;
            paymentsQuery = paymentsQuery.Where(x => x.Loan.BorrowerId == ownMemberId);
        }

        var payments = await paymentsQuery
            .Join(dbContext.Members,
                x => x.Loan.BorrowerId,
                m => m.Id,
                (x, m) => new
                {
                    x.Payment,
                    x.Loan,
                    BorrowerName = m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName
                })
            .OrderByDescending(x => x.Payment.PaidDate)
            .ThenByDescending(x => x.Payment.CreatedAt)
            .Select(x => new LoanPaymentListItemResponse(
                x.Payment.Id,
                x.Payment.LoanId,
                x.Loan.BorrowerId,
                x.BorrowerName,
                x.Loan.GroupId,
                x.Payment.Amount,
                x.Payment.PrincipalAmount,
                x.Payment.InterestAmount,
                x.Payment.PaidDate,
                x.Payment.PaymentType,
                x.Payment.Notes,
                x.Payment.IsVerified,
                x.Payment.VerifiedAt,
                x.Payment.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(payments);
    }
}
