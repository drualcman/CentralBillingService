using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.Persistence.SqlServer.Migrations.SqlInvoiceContext
{
    /// <inheritdoc />
    public partial class AddClientNumberPrefixSuffix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientNumberPrefix",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientNumberSuffix",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientNumberPrefix",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientNumberSuffix",
                table: "Invoices");
        }
    }
}
