using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MergeNonMembersIntoMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_members_email_group_id",
                schema: "public",
                table: "members");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "members",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "members",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "address",
                schema: "public",
                table: "members",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "member_type",
                schema: "public",
                table: "members",
                type: "text",
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.Sql(@"
                INSERT INTO public.members (id, first_name, last_name, email, phone_number, address, group_id, role, member_type, is_active, created_at)
                SELECT id, full_name, NULL, email, phone, address, group_id, 'NonMember', 'NonMember', is_active, created_at
                FROM public.non_members;
            ");

            migrationBuilder.DropTable(
                name: "non_members",
                schema: "public");

            migrationBuilder.CreateIndex(
                name: "ix_members_email_group_id",
                schema: "public",
                table: "members",
                columns: new[] { "email", "group_id" },
                unique: true,
                filter: "email IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_members_email_group_id",
                schema: "public",
                table: "members");

            migrationBuilder.DropColumn(
                name: "address",
                schema: "public",
                table: "members");

            migrationBuilder.DropColumn(
                name: "member_type",
                schema: "public",
                table: "members");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "public",
                table: "members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "public",
                table: "members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "members",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "non_members",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_non_members", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_members_email_group_id",
                schema: "public",
                table: "members",
                columns: new[] { "email", "group_id" },
                unique: true);
        }
    }
}
