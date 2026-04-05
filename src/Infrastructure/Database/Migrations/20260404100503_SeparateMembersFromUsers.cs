using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeparateMembersFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "member_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "members",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    signup_token = table.Column<Guid>(type: "uuid", nullable: true),
                    signup_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_members_email_group_id",
                schema: "public",
                table: "members",
                columns: new[] { "email", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_signup_token",
                schema: "public",
                table: "members",
                column: "signup_token",
                unique: true,
                filter: "signup_token IS NOT NULL");

            // Data migration: create Member records for all existing Member/Admin users
            // using the same ID so existing deposit/loan FKs remain valid.
            migrationBuilder.Sql("""
                INSERT INTO public.members (id, first_name, last_name, email, phone_number, group_id, role, is_active, signup_token, signup_token_expires_at, user_id, created_at)
                SELECT id, first_name, last_name, email, NULL, group_id, role, is_active, NULL, NULL, id, created_at
                FROM public.users
                WHERE role IN ('Member', 'Admin');
                """);

            // Set member_id on users to their own ID for migrated users
            migrationBuilder.Sql("""
                UPDATE public.users
                SET member_id = id
                WHERE role IN ('Member', 'Admin');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "members",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "member_id",
                schema: "public",
                table: "users");
        }
    }
}
