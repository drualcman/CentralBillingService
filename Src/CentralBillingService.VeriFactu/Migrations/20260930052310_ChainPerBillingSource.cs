using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.VeriFactu.Migrations
{
    /// <inheritdoc />
    public partial class ChainPerBillingSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VeriFactuChain",
                table: "VeriFactuChain");

            migrationBuilder.AddColumn<string>(
                name: "BillingSource",
                table: "VeriFactuChain",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VeriFactuChain",
                table: "VeriFactuChain",
                columns: new[] { "Nif", "BillingSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VeriFactuChain",
                table: "VeriFactuChain");

            migrationBuilder.DropColumn(
                name: "BillingSource",
                table: "VeriFactuChain");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VeriFactuChain",
                table: "VeriFactuChain",
                column: "Nif");
        }
    }
}
