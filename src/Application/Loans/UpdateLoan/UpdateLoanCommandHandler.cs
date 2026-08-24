using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
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

        var hasApprovals = await dbContext.LoanApprovals
            .AnyAsync(a => a.LoanId == command.LoanId, cancellationToken);

        if (hasApprovals)
            return Result.Failure(LoanErrors.CannotModifyApproved);

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

        var result = loan.Update(command.Amount, interestRate, command.DueDate, command.Notes);
        if (result.IsFailure)
            return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
