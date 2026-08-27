using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanRequestedAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "requested_amount",
                schema: "public",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Every loan that already exists was disbursed at the figure it asked for --
            // nothing could have reduced it before this column existed. Leaving the 0 default
            // would have every historical loan claiming it was approved for nothing.
            migrationBuilder.Sql(
                """
                UPDATE public.loans SET requested_amount = amount;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requested_amount",
                schema: "public",
                table: "loans");
        }
    }
}
