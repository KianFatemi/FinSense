using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinanceDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidTransactionIdToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaidTransactionId",
                table: "Transactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaidTransactionId",
                table: "Transactions");
        }
    }
}
