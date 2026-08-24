using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.CastVote;

internal sealed class CastLoanVoteCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CastLoanVoteCommand>
{
    public async Task<Result> Handle(CastLoanVoteCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        // Admins vote like any other member; only non-members are excluded.
        var isEligibleVoter = await LoanVoting.EligibleVoters(dbContext)
            .AnyAsync(m => m.Id == userContext.ActiveMemberId && m.GroupId == loan.GroupId, cancellationToken);

        if (!isEligibleVoter)
            return Result.Failure(LoanErrors.NotEligibleToVote);

        if (loan.Status != LoanStatus.Pending)
            return Result.Failure(LoanErrors.NotPending);

        // Members vote on every loan the group makes, whether the borrower is a
        // member or a non-member an admin added on their behalf.
        if (loan.BorrowerId == userContext.ActiveMemberId)
            return Result.Failure(LoanErrors.CannotSelfVote);

        var alreadyVoted = await dbContext.LoanApprovals
            .AnyAsync(a => a.LoanId == command.LoanId && a.ApproverId == userContext.UserId, cancellationToken);

        if (alreadyVoted)
            return Result.Failure(LoanErrors.AlreadyVoted);

        var vote = LoanApproval.Create(command.LoanId, userContext.UserId, command.IsApproved);
        dbContext.LoanApprovals.Add(vote);

        // Save the vote, then check whether it tips the balance
        await dbContext.SaveChangesAsync(cancellationToken);

        var totalVoters = await LoanVoting.EligibleVoters(dbContext)
            .CountAsync(m => m.GroupId == loan.GroupId, cancellationToken);

        var matchingVotes = await dbContext.LoanApprovals
            .CountAsync(a => a.LoanId == command.LoanId && a.IsApproved == command.IsApproved, cancellationToken);

        if (matchingVotes >= LoanVoting.VotesNeeded(totalVoters))
        {
            var result = Settle(loan, command.IsApproved);
            if (result.IsFailure) return result;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private static Result Settle(Loan loan, bool isApproved) =>
        isApproved ? loan.ApproveLoan() : loan.Decline();
}
