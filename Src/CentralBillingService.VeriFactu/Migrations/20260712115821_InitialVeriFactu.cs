using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.VeriFactu.Migrations
{
    /// <inheritdoc />
    public partial class InitialVeriFactu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VeriFactuChain",
                columns: table => new
                {
                    Nif = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    LastNumSerie = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LastFechaExpedicion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LastHuella = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeriFactuChain", x => x.Nif);
                });

            migrationBuilder.CreateTable(
                name: "VeriFactuSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IssuerNif = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Huella = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreviousHuella = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RegistroXml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Csv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErrorDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeriFactuSubmission", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VeriFactuSubmission_BillingSource_InvoiceNumber",
                table: "VeriFactuSubmission",
                columns: new[] { "BillingSource", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeriFactuSubmission_State",
                table: "VeriFactuSubmission",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeriFactuChain");

            migrationBuilder.DropTable(
                name: "VeriFactuSubmission");
        }
    }
}
