using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<Group> Groups { get; }
    DbSet<User> Users { get; }
    DbSet<Member> Members { get; }
    DbSet<Deposit> Deposits { get; }
    DbSet<Loan> Loans { get; }
    DbSet<LoanPayment> LoanPayments { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<LoanApproval> LoanApprovals { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<FixedDeposit> FixedDeposits { get; }
    DbSet<OtherIncomingFund> OtherIncomingFunds { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
