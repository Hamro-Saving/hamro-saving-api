using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Restates the backfilled deposit entries so they name the Bikram Sambat month the
    /// deposit covers, matching what the app writes for new ones. A monthly deposit is
    /// identified by its period, not by the day it happened to be paid.
    /// </summary>
    public partial class DescribeDepositsByBsPeriod : Migration
    {
        private const string BsMonths =
            "ARRAY['Baishakh','Jestha','Ashadh','Shrawan','Bhadra','Ashwin','Kartik','Mangsir','Poush','Magh','Falgun','Chaitra']";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE public.ledger_entries e
                SET description = CASE
                        WHEN d.type = 'MonthlyDeposit'
                            THEN 'Monthly deposit for '
                                 || ({BsMonths})[d.deposit_month] || ' ' || d.deposit_year
                        WHEN d.type = 'InterestPayment' THEN 'Interest payment received'
                        WHEN d.type = 'LoanRepayment'   THEN 'Loan repayment received'
                        ELSE 'Deposit received'
                    END
                FROM public.deposits d
                WHERE e.source_type = 'Deposit'
                  AND e.source_id = d.id
                  AND d.deposit_month BETWEEN 1 AND 12;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE public.ledger_entries e
                SET description = 'Deposit verified (' || d.type || ')'
                FROM public.deposits d
                WHERE e.source_type = 'Deposit' AND e.source_id = d.id;
                """);
        }
    }
}
