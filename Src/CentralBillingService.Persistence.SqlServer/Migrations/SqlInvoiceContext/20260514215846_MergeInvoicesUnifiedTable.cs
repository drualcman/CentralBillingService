using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.Persistence.SqlServer.Migrations.SqlInvoiceContext
{
    /// <inheritdoc />
    public partial class MergeInvoicesUnifiedTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_RectificativeInvoices_RectificativeInvoiceId",
                table: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "RectificativeInvoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_RectificativeInvoiceId_LineNumber",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "RectificativeInvoiceId",
                table: "InvoiceLines");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceType",
                table: "Invoices",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "F");

            migrationBuilder.AddColumn<string>(
                name: "OriginalInvoiceNumber",
                table: "Invoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OriginalIssueDate",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientExternalId",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RectificationReason",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RectificationType",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillingSource_RecipientExternalId",
                table: "Invoices",
                columns: new[] { "BillingSource", "RecipientExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceType",
                table: "Invoices",
                column: "InvoiceType");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OriginalInvoiceNumber",
                table: "Invoices",
                column: "OriginalInvoiceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_BillingSource_RecipientExternalId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceType",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OriginalInvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalIssueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RecipientExternalId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RectificationReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RectificationType",
                table: "Invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "RectificativeInvoiceId",
                table: "InvoiceLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RectificativeInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExchangeRateFetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExchangeRateFrom = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateTo = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateValue = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuerAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssuerAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IssuerPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IssuerProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuerTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssuerTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OriginCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OriginalInvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalIssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecipientAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RecipientPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecipientProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RectificationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RectificationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RectifiedByNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxableBaseEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalOriginAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalTaxAmountEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TransactionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RectificativeInvoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_RectificativeInvoiceId_LineNumber",
                table: "InvoiceLines",
                columns: new[] { "RectificativeInvoiceId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RectificativeInvoices_BillingSource_Serie_Year_SequenceNumber",
                table: "RectificativeInvoices",
                columns: new[] { "BillingSource", "Serie", "Year", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RectificativeInvoices_InvoiceNumber",
                table: "RectificativeInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RectificativeInvoices_OriginalInvoiceNumber",
                table: "RectificativeInvoices",
                column: "OriginalInvoiceNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_RectificativeInvoices_RectificativeInvoiceId",
                table: "InvoiceLines",
                column: "RectificativeInvoiceId",
                principalTable: "RectificativeInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
