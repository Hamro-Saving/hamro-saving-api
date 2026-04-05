using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserMemberRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_members_signup_token",
                schema: "public",
                table: "members");

            migrationBuilder.DropColumn(
                name: "signup_token",
                schema: "public",
                table: "members");

            migrationBuilder.DropColumn(
                name: "signup_token_expires_at",
                schema: "public",
                table: "members");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "invite_token",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "invite_token_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "member_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_invite_token",
                schema: "public",
                table: "users",
                column: "invite_token",
                unique: true,
                filter: "invite_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_member_id",
                schema: "public",
                table: "users",
                column: "member_id",
                unique: true,
                filter: "member_id IS NOT NULL");

            // Data migration: member_id now exists on users, and user_id still exists on members — copy the link
            migrationBuilder.Sql("""
                UPDATE public.users u
                SET member_id = (SELECT m.id FROM public.members m WHERE m.user_id = u.id)
                WHERE EXISTS (SELECT 1 FROM public.members m WHERE m.user_id = u.id);
                """);

            // Keep existing regular users active (new users default to inactive until invite accepted)
            migrationBuilder.Sql("""
                UPDATE public.users
                SET is_active = true
                WHERE member_id IS NOT NULL;
                """);

            // Now safe to drop user_id from members (data has been migrated)
            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "public",
                table: "members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_invite_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_member_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "invite_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "invite_token_expires_at",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "member_id",
                schema: "public",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "signup_token",
                schema: "public",
                table: "members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "signup_token_expires_at",
                schema: "public",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "public",
                table: "members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_signup_token",
                schema: "public",
                table: "members",
                column: "signup_token",
                unique: true,
                filter: "signup_token IS NOT NULL");
        }
    }
}
