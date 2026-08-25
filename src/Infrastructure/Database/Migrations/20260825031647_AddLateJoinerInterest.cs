using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLateJoinerInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "late_joiner_interests",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_late_joiner_interests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_late_joiner_interests_group_id_paid_date",
                schema: "public",
                table: "late_joiner_interests",
                columns: new[] { "group_id", "paid_date" });

            migrationBuilder.CreateIndex(
                name: "ix_late_joiner_interests_member_id",
                schema: "public",
                table: "late_joiner_interests",
                column: "member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "late_joiner_interests",
                schema: "public");
        }
    }
}
