using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.ApproveLoan;

internal sealed class ApproveLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<ApproveLoanCommand>
{
    public async Task<Result> Handle(ApproveLoanCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.IsSuperAdmin && loan.GroupId != userContext.GroupId)
            return Result.Failure(LoanErrors.NotInGroup);

        if (loan.Status != LoanStatus.Pending)
            return Result.Failure(LoanErrors.NotPending);

        if (loan.BorrowerType != "Member")
            return Result.Failure(LoanErrors.ApprovalNotApplicable);

        if (loan.BorrowerId == userContext.UserId && loan.BorrowerType == "Member")
            return Result.Failure(LoanErrors.CannotSelfApprove);

        var alreadyApproved = await dbContext.LoanApprovals
            .AnyAsync(a => a.LoanId == command.LoanId && a.ApproverId == userContext.UserId, cancellationToken);

        if (alreadyApproved)
            return Result.Failure(LoanErrors.AlreadyApproved);

        // Record approval for everyone (admin or member)
        var approval = LoanApproval.Create(command.LoanId, userContext.UserId);
        dbContext.LoanApprovals.Add(approval);

        // Admin approval instantly moves loan to Approved state
        if (userContext.IsAdmin || userContext.IsSuperAdmin)
        {
            var adminResult = loan.ApproveLoan();
            if (adminResult.IsFailure) return adminResult;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Member vote path — save the vote, then check threshold
        await dbContext.SaveChangesAsync(cancellationToken);

        var approvalCount = await dbContext.LoanApprovals
            .CountAsync(a => a.LoanId == command.LoanId, cancellationToken);

        var totalMembers = await dbContext.Users
            .CountAsync(u => u.GroupId == userContext.GroupId && u.Role == UserRole.Member, cancellationToken);

        if (approvalCount > totalMembers / 2.0)
        {
            loan.ApproveLoan();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
