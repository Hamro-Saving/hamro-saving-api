using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.RecordPayment;

internal sealed class RecordLoanPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<RecordLoanPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordLoanPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsAdmin && !userContext.IsSuperAdmin)
            return Result.Failure<Guid>(UserErrors.Unauthorized);

        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(UserErrors.NotInGroup);
        }

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
        {
            return Result.Failure<Guid>(LoanErrors.NotFound(command.LoanId));
        }

        if (!userContext.IsSuperAdmin && loan.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(LoanErrors.NotInGroup);
        }

        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
        {
            return Result.Failure<Guid>(LoanErrors.NotActive);
        }

        var payment = LoanPayment.Create(
            command.LoanId,
            command.Amount,
            command.PrincipalAmount,
            command.InterestAmount,
            command.PaidDate,
            command.PaymentType,
            command.Notes,
            userContext.UserId);

        dbContext.LoanPayments.Add(payment);

        var totalPaid = await dbContext.LoanPayments
            .Where(p => p.LoanId == command.LoanId)
            .SumAsync(p => p.PrincipalAmount, cancellationToken);

        totalPaid += command.PrincipalAmount;

        if (totalPaid >= loan.Amount)
        {
            loan.MarkAsPaidOff();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
