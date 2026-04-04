using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loan_approvals",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_approvals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_loan_approvals_loan_id_approver_id",
                schema: "public",
                table: "loan_approvals",
                columns: new[] { "loan_id", "approver_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_approvals",
                schema: "public");
        }
    }
}
