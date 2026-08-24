using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Folds membership_type into group_role. The two were never independent — a NonMember could
    /// never be an admin, so only three of the four combinations were ever valid. NonMember becomes
    /// the third group role, and the column carrying the fourth, impossible one goes away.
    /// </summary>
    public partial class MergeMembershipTypeIntoGroupRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Carry the distinction over before the column holding it is dropped.
            migrationBuilder.Sql("""
                UPDATE public.members
                SET group_role = 'NonMember'
                WHERE membership_type = 'NonMember';
                """);

            migrationBuilder.DropColumn(
                name: "membership_type",
                schema: "public",
                table: "members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "membership_type",
                schema: "public",
                table: "members",
                type: "text",
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.Sql("""
                UPDATE public.members
                SET membership_type = CASE WHEN group_role = 'NonMember' THEN 'NonMember' ELSE 'Member' END;
                """);

            // The old shape had no role for a non-member; they sat at 'Member'.
            migrationBuilder.Sql("""
                UPDATE public.members
                SET group_role = 'Member'
                WHERE group_role = 'NonMember';
                """);
        }
    }
}
