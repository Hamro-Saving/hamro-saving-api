using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "deposit_date",
                schema: "public",
                table: "deposits",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deposit_date",
                schema: "public",
                table: "deposits");
        }
    }
}
