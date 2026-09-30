using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.Persistence.SqlServer.Migrations.SqlInvoiceContext
{
    /// <inheritdoc />
    public partial class AddInvoiceFiscalQrContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalQrContent",
                table: "Invoices",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalQrContent",
                table: "Invoices");
        }
    }
}
