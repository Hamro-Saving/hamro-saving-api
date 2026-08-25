using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroSavings.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Only a monthly deposit covers a Bikram Sambat period. The columns were required, so
    /// every other kind of deposit carried a month and year nobody chose — and the deposits
    /// list showed them as if they meant something. They become nullable, and the invented
    /// values are cleared.
    /// </summary>
    public partial class DepositPeriodOnlyForMonthly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "deposit_year",
                schema: "public",
                table: "deposits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "deposit_month",
                schema: "public",
                table: "deposits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql("""
                UPDATE public.deposits
                SET deposit_month = NULL, deposit_year = NULL
                WHERE type <> 'MonthlyDeposit';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The columns cannot go back to NOT NULL while any row holds a null, so the
            // cleared periods are restamped from the date the deposit was recorded.
            migrationBuilder.Sql("""
                UPDATE public.deposits
                SET deposit_month = COALESCE(deposit_month, EXTRACT(MONTH FROM deposit_date)::int),
                    deposit_year  = COALESCE(deposit_year,  EXTRACT(YEAR  FROM deposit_date)::int)
                WHERE deposit_month IS NULL OR deposit_year IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "deposit_year",
                schema: "public",
                table: "deposits",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "deposit_month",
                schema: "public",
                table: "deposits",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
