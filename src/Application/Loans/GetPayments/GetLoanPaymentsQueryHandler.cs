using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.GetPayments;

internal sealed class GetLoanPaymentsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetLoanPaymentsQuery, List<LoanPaymentResponse>>
{
    public async Task<Result<List<LoanPaymentResponse>>> Handle(GetLoanPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == query.LoanId, cancellationToken);

        if (loan is null)
        {
            return Result.Failure<List<LoanPaymentResponse>>(LoanErrors.NotFound(query.LoanId));
        }

        if (!userContext.CanRead(loan.GroupId))
        {
            return Result.Failure<List<LoanPaymentResponse>>(LoanErrors.NotInGroup);
        }

        // A non-member follows their own loan and no one else's.
        if (userContext.SeesOnlyOwnRecords() && loan.BorrowerId != userContext.ActiveMemberId)
        {
            return Result.Failure<List<LoanPaymentResponse>>(LoanErrors.NotInGroup);
        }

        var payments = await dbContext.LoanPayments
            .Where(p => p.LoanId == query.LoanId)
            .OrderByDescending(p => p.PaidDate)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new LoanPaymentResponse(
                p.Id,
                p.LoanId,
                p.Amount,
                p.PrincipalAmount,
                p.InterestAmount,
                p.PaidDate,
                p.PaymentType,
                p.Notes,
                p.InterestOwedBefore,
                p.DaysAccrued,
                p.OutstandingPrincipalAfter,
                p.UnpaidInterestAfter,
                p.IsVerified,
                p.VerifiedAt,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(payments);
    }
}
