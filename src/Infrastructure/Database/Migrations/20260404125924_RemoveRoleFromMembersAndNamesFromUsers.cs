using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoleFromMembersAndNamesFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "public",
                table: "members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "public",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "public",
                table: "members",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
