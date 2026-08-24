using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Splits the single users.role column into two independent axes: users.is_super_admin for the
    /// platform, and members.group_role for one group. The User -> Member link is reversed so a
    /// person can hold a membership, and a different role, in more than one group.
    /// New columns are filled from the old ones before the old ones are dropped.
    /// </summary>
    public partial class SplitPlatformAndGroupRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_super_admin",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "group_role",
                schema: "public",
                table: "members",
                type: "text",
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "public",
                table: "members",
                type: "uuid",
                nullable: true);

            // Reverse the link: each member row picks up the user that used to point at it.
            migrationBuilder.Sql("""
                UPDATE public.members m
                SET user_id = u.id
                FROM public.users u
                WHERE u.member_id = m.id;
                """);

            // A user whose global role was Admin was, in practice, the admin of their one group.
            migrationBuilder.Sql("""
                UPDATE public.members m
                SET group_role = 'Admin'
                FROM public.users u
                WHERE u.member_id = m.id AND u.role = 'Admin';
                """);

            migrationBuilder.Sql("""
                UPDATE public.users
                SET is_super_admin = true
                WHERE role = 'SuperAdmin';
                """);

            migrationBuilder.DropIndex(
                name: "ix_users_member_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "member_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "public",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_members_group_id",
                schema: "public",
                table: "members",
                column: "group_id");

            // One membership per person per group.
            migrationBuilder.CreateIndex(
                name: "ix_members_user_id_group_id",
                schema: "public",
                table: "members",
                columns: new[] { "user_id", "group_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_members_groups_group_id",
                schema: "public",
                table: "members",
                column: "group_id",
                principalSchema: "public",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_members_users_user_id",
                schema: "public",
                table: "members",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_members_groups_group_id",
                schema: "public",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "fk_members_users_user_id",
                schema: "public",
                table: "members");

            migrationBuilder.DropIndex(
                name: "ix_members_group_id",
                schema: "public",
                table: "members");

            migrationBuilder.DropIndex(
                name: "ix_members_user_id_group_id",
                schema: "public",
                table: "members");

            migrationBuilder.AddColumn<Guid>(
                name: "member_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "public",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Member");

            // Collapse back to one role and one group per user. A person who by then belongs to
            // several groups keeps only their oldest membership, which is all the old shape held.
            migrationBuilder.Sql("""
                UPDATE public.users u
                SET member_id = m.id,
                    role = CASE WHEN m.group_role = 'Admin' THEN 'Admin' ELSE 'Member' END
                FROM (
                    SELECT DISTINCT ON (user_id) user_id, id, group_role
                    FROM public.members
                    WHERE user_id IS NOT NULL
                    ORDER BY user_id, created_at
                ) m
                WHERE m.user_id = u.id;
                """);

            migrationBuilder.Sql("""
                UPDATE public.users
                SET role = 'SuperAdmin'
                WHERE is_super_admin = true;
                """);

            migrationBuilder.DropColumn(
                name: "is_super_admin",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "group_role",
                schema: "public",
                table: "members");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "public",
                table: "members");

            migrationBuilder.CreateIndex(
                name: "ix_users_member_id",
                schema: "public",
                table: "users",
                column: "member_id",
                unique: true,
                filter: "member_id IS NOT NULL");
        }
    }
}
