using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.Persistence.SqlServer.Migrations.SqlInvoiceContext
{
    /// <inheritdoc />
    public partial class DropExchangeRateSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "RectificativeInvoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "RectificativeInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
