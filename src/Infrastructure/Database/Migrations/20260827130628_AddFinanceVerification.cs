using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "approved_by_id",
                schema: "public",
                table: "expenses",
                newName: "verified_by_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                schema: "public",
                table: "other_incoming_funds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                schema: "public",
                table: "other_incoming_funds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_id",
                schema: "public",
                table: "other_incoming_funds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                schema: "public",
                table: "fixed_deposits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_withdrawal_verified",
                schema: "public",
                table: "fixed_deposits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                schema: "public",
                table: "fixed_deposits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_id",
                schema: "public",
                table: "fixed_deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "withdrawal_verified_at",
                schema: "public",
                table: "fixed_deposits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "withdrawal_verified_by_id",
                schema: "public",
                table: "fixed_deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                schema: "public",
                table: "expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                schema: "public",
                table: "expenses",
                type: "timestamp with time zone",
                nullable: true);

            // Existing rows were posted to the ledger when they were created — the behaviour
            // this change replaces. Left unverified, verifying one would post it a second time
            // and double every historical figure. Backfilled as verified by whoever recorded
            // them, which is what happened under the old rules.
            migrationBuilder.Sql("""
                UPDATE public.expenses
                SET is_verified = TRUE,
                    verified_at = created_at,
                    verified_by_id = created_by_id;
                """);

            migrationBuilder.Sql("""
                UPDATE public.other_incoming_funds
                SET is_verified = TRUE,
                    verified_at = created_at,
                    verified_by_id = recorded_by_id;
                """);

            migrationBuilder.Sql("""
                UPDATE public.fixed_deposits
                SET is_verified = TRUE,
                    verified_at = created_at,
                    verified_by_id = created_by_id;
                """);

            // Only a withdrawn deposit had its return posted. Status is stored as a string.
            migrationBuilder.Sql("""
                UPDATE public.fixed_deposits
                SET is_withdrawal_verified = TRUE,
                    withdrawal_verified_at = withdrawn_at,
                    withdrawal_verified_by_id = withdrawn_by_id
                WHERE status = 'Withdrawn';
                """);
        }

        /// <inheritdoc />
        // The backfill needs no counterpart: every column it wrote is dropped below.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_verified",
                schema: "public",
                table: "other_incoming_funds");

            migrationBuilder.DropColumn(
                name: "verified_at",
                schema: "public",
                table: "other_incoming_funds");

            migrationBuilder.DropColumn(
                name: "verified_by_id",
                schema: "public",
                table: "other_incoming_funds");

            migrationBuilder.DropColumn(
                name: "is_verified",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "is_withdrawal_verified",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "verified_at",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "verified_by_id",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "withdrawal_verified_at",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "withdrawal_verified_by_id",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "is_verified",
                schema: "public",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "verified_at",
                schema: "public",
                table: "expenses");

            migrationBuilder.RenameColumn(
                name: "verified_by_id",
                schema: "public",
                table: "expenses",
                newName: "approved_by_id");
        }
    }
}
