using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "password_reset_token",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_password_reset_token",
                schema: "public",
                table: "users",
                column: "password_reset_token",
                unique: true,
                filter: "password_reset_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_password_reset_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expires_at",
                schema: "public",
                table: "users");
        }
    }
}
