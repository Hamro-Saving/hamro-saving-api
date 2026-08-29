using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.CreateLoan;

internal sealed class CreateLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateLoanCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLoanCommand command, CancellationToken cancellationToken = default)
    {
        // Admins and members act in the group on their token; only a SuperAdmin names one
        var groupResult = userContext.ResolveWriteGroupId();
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        // Members (non-admin) can only apply for themselves
        if (!userContext.IsGroupAdmin)
        {
            if (command.BorrowerType != "Member" || command.BorrowerId != userContext.ActiveMemberId)
                return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));
        }

        if (command.BorrowerType is not ("Member" or "NonMember"))
        {
            return Result.Failure<Guid>(Error.Validation("Loan.InvalidBorrowerType", "BorrowerType must be 'Member' or 'NonMember'."));
        }

        var lendingToNonMember = command.BorrowerType == "NonMember";

        var borrower = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.BorrowerId && m.GroupId == groupId, cancellationToken);

        // The role has to match the one being claimed, or the loan would be priced at the
        // other one's rate.
        if (borrower is null || (borrower.GroupRole == GroupRole.NonMember) != lendingToNonMember)
        {
            return Result.Failure<Guid>(MemberErrors.NotFound(command.BorrowerId));
        }

        // Deactivating someone is the group saying it will not lend to them again — which for
        // a non-member, who has no standing in the group to lose, is the whole of what it means.
        if (!borrower.IsActive)
        {
            return Result.Failure<Guid>(MemberErrors.InactiveBorrower);
        }

        // Only admins may override the group default rate
        var effectiveRate = userContext.IsGroupAdmin ? command.InterestRate : null;
        var interestRate = effectiveRate
            ?? (lendingToNonMember ? group.NonMemberInterestRate : group.MemberInterestRate);


        // The rule about what the group may commit lives on CashInHand; the balance is
        // read from the books here.
        var inHand = await CashPosition.InHandAsync(dbContext, groupId, cancellationToken);
        var covered = inHand.EnsureCovers(command.Amount);
        if (covered.IsFailure) return Result.Failure<Guid>(covered.Error);

        var loan = Loan.Create(
            command.BorrowerId,
            command.BorrowerType,
            groupId,
            command.Amount,
            interestRate,
            command.StartDate,
            command.DueDate,
            command.Notes);

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(loan.Id);
    }
}
