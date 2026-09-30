using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.VeriFactu.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionChainFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FechaHoraGenRegistro",
                table: "VeriFactuSubmission",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousFechaExpedicion",
                table: "VeriFactuSubmission",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousNumSerie",
                table: "VeriFactuSubmission",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaHoraGenRegistro",
                table: "VeriFactuSubmission");

            migrationBuilder.DropColumn(
                name: "PreviousFechaExpedicion",
                table: "VeriFactuSubmission");

            migrationBuilder.DropColumn(
                name: "PreviousNumSerie",
                table: "VeriFactuSubmission");
        }
    }
}
