using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PersonalFinanceDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Securities",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaidSecurityId = table.Column<string>(type: "text", nullable: true),
                    TickerStymbol = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    ClosePrice = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ClosePriceAsOf = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Securities", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Holdings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialAccountId = table.Column<int>(type: "integer", nullable: false),
                    SecurityId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    CostBasis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InstitutionValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holdings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Holdings_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Holdings_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_FinancialAccountId",
                table: "Holdings",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_SecurityId",
                table: "Holdings",
                column: "SecurityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holdings");

            migrationBuilder.DropTable(
                name: "Securities");
        }
    }
}
