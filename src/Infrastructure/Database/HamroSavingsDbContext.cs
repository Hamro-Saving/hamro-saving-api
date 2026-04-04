using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Infrastructure.Database;

public sealed class HamroSavingsDbContext(DbContextOptions<HamroSavingsDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Group> Groups { get; init; }
    public DbSet<User> Users { get; init; }
    public DbSet<NonMember> NonMembers { get; init; }
    public DbSet<Deposit> Deposits { get; init; }
    public DbSet<Loan> Loans { get; init; }
    public DbSet<LoanPayment> LoanPayments { get; init; }
    public DbSet<Expense> Expenses { get; init; }
    public DbSet<FixedDeposit> FixedDeposits { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HamroSavingsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }
}
