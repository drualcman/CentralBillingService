using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.VeriFactu.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionInvoiceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceType",
                table: "VeriFactuSubmission",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceType",
                table: "VeriFactuSubmission");
        }
    }
}
