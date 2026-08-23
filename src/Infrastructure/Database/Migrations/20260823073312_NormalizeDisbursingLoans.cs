using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDisbursingLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The short-lived Disbursing status is gone; any loan left in it goes back to
            // Approved, where an admin marks the disbursement complete in one step.
            migrationBuilder.Sql(
                "UPDATE public.loans SET status = 'Approved' WHERE status = 'Disbursing';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
