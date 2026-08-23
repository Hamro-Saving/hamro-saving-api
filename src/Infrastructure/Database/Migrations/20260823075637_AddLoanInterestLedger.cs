using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanInterestLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "disbursed_at",
                schema: "public",
                table: "loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_accrual_date",
                schema: "public",
                table: "loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "outstanding_principal",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_interest_accrued",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_interest_paid",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_principal_paid",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unpaid_interest",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "days_accrued",
                schema: "public",
                table: "loan_payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "interest_owed_before",
                schema: "public",
                table: "loan_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "outstanding_principal_after",
                schema: "public",
                table: "loan_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unpaid_interest_after",
                schema: "public",
                table: "loan_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Seed the ledger for loans that already exist. Interest before this point was never
            // tracked per day, so accrual restarts from each loan's last transaction: existing
            // payments keep their recorded split, and no historic interest is invented.
            migrationBuilder.Sql("""
                UPDATE public.loans l
                SET total_principal_paid = p.principal_paid,
                    total_interest_paid  = p.interest_paid,
                    total_interest_accrued = p.interest_paid
                FROM (
                    SELECT loan_id,
                           COALESCE(SUM(principal_amount), 0) AS principal_paid,
                           COALESCE(SUM(interest_amount), 0)  AS interest_paid
                    FROM public.loan_payments
                    GROUP BY loan_id
                ) p
                WHERE p.loan_id = l.id;

                UPDATE public.loans l
                SET disbursed_at = COALESCE(l.disbursed_at, l.start_date),
                    last_accrual_date = COALESCE(
                        (SELECT MAX(paid_date) FROM public.loan_payments p WHERE p.loan_id = l.id),
                        l.start_date),
                    outstanding_principal = GREATEST(l.amount - l.total_principal_paid, 0)
                WHERE l.status IN ('Active', 'Overdue');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disbursed_at",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "last_accrual_date",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "outstanding_principal",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "total_interest_accrued",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "total_interest_paid",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "total_principal_paid",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "unpaid_interest",
                schema: "public",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "days_accrued",
                schema: "public",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "interest_owed_before",
                schema: "public",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "outstanding_principal_after",
                schema: "public",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "unpaid_interest_after",
                schema: "public",
                table: "loan_payments");
        }
    }
}
