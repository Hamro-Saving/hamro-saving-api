using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    debit_account = table.Column<string>(type: "text", nullable: false),
                    credit_account = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_entries_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_group_id_occurred_at",
                schema: "public",
                table: "ledger_entries",
                columns: new[] { "group_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_member_id",
                schema: "public",
                table: "ledger_entries",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_source_type_source_id",
                schema: "public",
                table: "ledger_entries",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_source_type_source_id_type",
                schema: "public",
                table: "ledger_entries",
                columns: new[] { "source_type", "source_id", "type" },
                unique: true);

            // Post the books for everything already recorded, so the ledger is complete
            // rather than starting blank. Each block mirrors exactly one posting rule in
            // LedgerPosting; amounts of zero are skipped, since they are not movements.
            migrationBuilder.Sql("""
                -- Deposits: cash in, and the group now owes that member more.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), d.group_id,
                       d.deposit_date::timestamptz, 'Deposit', 'Cash', 'MemberSavings', d.amount,
                       'Deposit verified (' || d.type || ')', d.member_id, 'Deposit', d.id, now()
                FROM public.deposits d
                WHERE d.is_verified AND d.amount > 0;

                -- Loans that actually paid out: cash became a receivable.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), l.group_id,
                       l.disbursed_at, 'LoanDisbursement', 'LoanReceivable', 'Cash', l.amount,
                       'Loan disbursed', l.borrower_id, 'Loan', l.id, now()
                FROM public.loans l
                WHERE l.disbursed_at IS NOT NULL AND l.amount > 0;

                -- Verified repayments split: capital back, then interest earned.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), l.group_id,
                       p.paid_date, 'LoanPrincipalPayment', 'Cash', 'LoanReceivable', p.principal_amount,
                       'Loan principal repaid', l.borrower_id, 'LoanPayment', p.id, now()
                FROM public.loan_payments p
                JOIN public.loans l ON l.id = p.loan_id
                WHERE p.is_verified AND p.principal_amount > 0;

                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), l.group_id,
                       p.paid_date, 'LoanInterestPayment', 'Cash', 'InterestIncome', p.interest_amount,
                       'Loan interest received', l.borrower_id, 'LoanPayment', p.id, now()
                FROM public.loan_payments p
                JOIN public.loans l ON l.id = p.loan_id
                WHERE p.is_verified AND p.interest_amount > 0;

                -- Fixed deposits: money placed with an institution.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), fd.group_id,
                       fd.start_date, 'FixedDepositPlaced', 'FixedDeposits', 'Cash', fd.amount,
                       'Fixed deposit placed with ' || fd.institution_name, NULL, 'FixedDeposit', fd.id, now()
                FROM public.fixed_deposits fd
                WHERE fd.amount > 0;

                -- ... and back out again, principal and interest posted separately.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), fd.group_id,
                       fd.withdrawn_at, 'FixedDepositWithdrawal', 'Cash', 'FixedDeposits', fd.amount,
                       'Fixed deposit withdrawn from ' || fd.institution_name, NULL, 'FixedDeposit', fd.id, now()
                FROM public.fixed_deposits fd
                WHERE fd.status = 'Withdrawn' AND fd.withdrawn_at IS NOT NULL AND fd.amount > 0;

                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), fd.group_id,
                       fd.withdrawn_at, 'FixedDepositInterest', 'Cash', 'InterestIncome', fd.interest_earned,
                       'Fixed deposit interest from ' || fd.institution_name, NULL, 'FixedDeposit', fd.id, now()
                FROM public.fixed_deposits fd
                WHERE fd.status = 'Withdrawn' AND fd.withdrawn_at IS NOT NULL
                  AND COALESCE(fd.interest_earned, 0) > 0;

                -- Expenses: money out and not coming back.
                INSERT INTO public.ledger_entries
                    (id, group_id, occurred_at, type, debit_account, credit_account, amount,
                     description, member_id, source_type, source_id, created_at)
                SELECT gen_random_uuid(), e.group_id,
                       e.expense_date, 'Expense', 'Expenses', 'Cash', e.amount,
                       e.category || ': ' || e.description, NULL, 'Expense', e.id, now()
                FROM public.expenses e
                WHERE e.amount > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "public");
        }
    }
}
