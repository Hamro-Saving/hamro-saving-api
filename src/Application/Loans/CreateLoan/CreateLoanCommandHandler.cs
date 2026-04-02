using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
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
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(UserErrors.NotInGroup);
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId));
        }

        decimal interestRate;
        if (command.BorrowerType == "Member")
        {
            var memberExists = await dbContext.Users
                .AnyAsync(u => u.Id == command.BorrowerId && u.Role == UserRole.Member && u.GroupId == command.GroupId, cancellationToken);
            if (!memberExists)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.BorrowerId));
            }
            interestRate = command.InterestRate ?? group.MemberInterestRate;
        }
        else if (command.BorrowerType == "NonMember")
        {
            var nonMemberExists = await dbContext.NonMembers
                .AnyAsync(nm => nm.Id == command.BorrowerId && nm.GroupId == command.GroupId, cancellationToken);
            if (!nonMemberExists)
            {
                return Result.Failure<Guid>(NonMemberErrors.NotFound(command.BorrowerId));
            }
            interestRate = command.InterestRate ?? group.NonMemberInterestRate;
        }
        else
        {
            return Result.Failure<Guid>(Error.Validation("Loan.InvalidBorrowerType", "BorrowerType must be 'Member' or 'NonMember'."));
        }

        var loan = Loan.Create(
            command.BorrowerId,
            command.BorrowerType,
            command.GroupId,
            command.Amount,
            interestRate,
            command.StartDate,
            command.DueDate,
            command.Notes,
            userContext.UserId);

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(loan.Id);
    }
}
