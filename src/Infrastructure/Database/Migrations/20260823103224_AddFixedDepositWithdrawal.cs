using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedDepositWithdrawal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "interest_earned",
                schema: "public",
                table: "fixed_deposits",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "withdrawn_at",
                schema: "public",
                table: "fixed_deposits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "withdrawn_by_id",
                schema: "public",
                table: "fixed_deposits",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interest_earned",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "withdrawn_at",
                schema: "public",
                table: "fixed_deposits");

            migrationBuilder.DropColumn(
                name: "withdrawn_by_id",
                schema: "public",
                table: "fixed_deposits");
        }
    }
}
