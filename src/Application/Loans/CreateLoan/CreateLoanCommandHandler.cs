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

        decimal interestRate;
        if (command.BorrowerType == "Member")
        {
            var memberExists = await dbContext.Members
                .AnyAsync(m => m.Id == command.BorrowerId && m.GroupId == groupId, cancellationToken);
            if (!memberExists)
            {
                return Result.Failure<Guid>(MemberErrors.NotFound(command.BorrowerId));
            }
            // Only admins may override the group default rate
            var effectiveRate = (userContext.IsGroupAdmin) ? command.InterestRate : null;
            interestRate = effectiveRate ?? group.MemberInterestRate;
        }
        else if (command.BorrowerType == "NonMember")
        {
            var nonMemberExists = await dbContext.Members
                .AnyAsync(nm => nm.Id == command.BorrowerId && nm.GroupId == groupId && nm.GroupRole == Domain.Members.GroupRole.NonMember, cancellationToken);
            if (!nonMemberExists)
            {
                return Result.Failure<Guid>(MemberErrors.NotFound(command.BorrowerId));
            }
            // Only admins may override the group default rate
            var effectiveRate = (userContext.IsGroupAdmin) ? command.InterestRate : null;
            interestRate = effectiveRate ?? group.NonMemberInterestRate;
        }
        else
        {
            return Result.Failure<Guid>(Error.Validation("Loan.InvalidBorrowerType", "BorrowerType must be 'Member' or 'NonMember'."));
        }


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
