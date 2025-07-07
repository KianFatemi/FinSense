using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceDashboard.Migrations
{
    /// <inheritdoc />
    public partial class FixSpelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TickerStymbol",
                table: "Securities",
                newName: "TickerSymbol");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostBasis",
                table: "Holdings",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TickerSymbol",
                table: "Securities",
                newName: "TickerStymbol");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostBasis",
                table: "Holdings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);
        }
    }
}
