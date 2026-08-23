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

        if (command.PaidDate.Date > DateTime.UtcNow.Date)
        {
            return Result.Failure<Guid>(LoanErrors.PaymentInFuture);
        }

        // The loan settles its own interest up to this date and tells us what the payment covered
        var allocation = loan.RecordPayment(command.PaidDate, command.PrincipalAmount, command.InterestAmount);
        if (allocation.IsFailure)
        {
            return Result.Failure<Guid>(allocation.Error);
        }

        var payment = LoanPayment.Create(
            command.LoanId,
            command.PaidDate,
            allocation.Value,
            command.Notes,
            userContext.UserId);

        dbContext.LoanPayments.Add(payment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
