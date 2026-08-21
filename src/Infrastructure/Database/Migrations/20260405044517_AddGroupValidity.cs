using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupValidity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "valid_from",
                schema: "public",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "valid_to",
                schema: "public",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valid_from",
                schema: "public",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "valid_to",
                schema: "public",
                table: "groups");
        }
    }
}
