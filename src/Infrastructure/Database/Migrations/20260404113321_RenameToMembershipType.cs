using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameToMembershipType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "member_type",
                schema: "public",
                table: "members",
                newName: "membership_type");

            // Role 'NonMember' is removed from UserRole; NonMember type members now use role 'Member'
            migrationBuilder.Sql("""
                UPDATE public.members SET role = 'Member' WHERE role = 'NonMember';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "membership_type",
                schema: "public",
                table: "members",
                newName: "member_type");
        }
    }
}
