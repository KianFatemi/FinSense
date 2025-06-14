using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceDashboard.Migrations
{
    /// <inheritdoc />
    public partial class FinancialAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaidAccountID",
                table: "FinancialAccounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaidAccountID",
                table: "FinancialAccounts");
        }
    }
}
