using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MakePasswordHashRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
