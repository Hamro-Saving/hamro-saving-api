using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.UpdateLoan;

internal sealed class UpdateLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateLoanCommand>
{
    public async Task<Result> Handle(UpdateLoanCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        // Members can only edit their own Member-type loans; admins can edit any in their group
        if (!userContext.IsGroupAdmin &&
            (loan.BorrowerType != "Member" || loan.BorrowerId != userContext.ActiveMemberId))
            return Result.Failure(UserErrors.Unauthorized);

        decimal interestRate;
        if (command.InterestRate.HasValue && (userContext.IsGroupAdmin))
        {
            interestRate = command.InterestRate.Value;
        }
        else
        {
            var group = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == loan.GroupId, cancellationToken);
            interestRate = loan.BorrowerType == "Member"
                ? (group?.MemberInterestRate ?? loan.InterestRate)
                : (group?.NonMemberInterestRate ?? loan.InterestRate);
        }

        var inHand = await CashPosition.InHandAsync(dbContext, loan.GroupId, cancellationToken);
        var covered = inHand.EnsureCovers(command.Amount);
        if (covered.IsFailure) return covered;

        var result = loan.Revise(command.Amount, interestRate, command.DueDate, command.Notes);
        if (result.IsFailure)
            return result;

        // Every vote was cast on the loan as it was before this edit, so none of them
        // carries over. The group looks at it again.
        var votes = await dbContext.LoanApprovals
            .Where(a => a.LoanId == command.LoanId)
            .ToListAsync(cancellationToken);

        dbContext.LoanApprovals.RemoveRange(votes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
