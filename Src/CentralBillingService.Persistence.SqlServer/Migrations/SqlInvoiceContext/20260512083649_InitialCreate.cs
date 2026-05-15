using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.Persistence.SqlServer.Migrations.SqlInvoiceContext
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BillingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssuerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IssuerWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuerProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuerPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IssuerAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RecipientWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecipientProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecipientAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaxableBaseEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalTaxAmountEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalOriginAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OriginCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateFrom = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateTo = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateValue = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ExchangeRateFetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RectifiedByNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    LastHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RectificativeInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BillingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OriginalInvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalIssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RectificationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RectificationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssuerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IssuerWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuerAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuerCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuerProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuerPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IssuerAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientTradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientTaxIdValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientTaxIdCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RecipientWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecipientProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecipientAddressCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaxableBaseEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalTaxAmountEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalOriginAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OriginCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateFrom = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateTo = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRateValue = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ExchangeRateSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExchangeRateFetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RectificativeInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RectificativeInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TaxRatePercentage = table.Column<int>(type: "int", nullable: false),
                    UnitPriceEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxableBaseEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmountEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalEur = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPriceOrigin = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalOrigin = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OriginCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    HasCurrencyConversion = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_RectificativeInvoices_RectificativeInvoiceId",
                        column: x => x.RectificativeInvoiceId,
                        principalTable: "RectificativeInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId_LineNumber",
                table: "InvoiceLines",
                columns: new[] { "InvoiceId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_RectificativeInvoiceId_LineNumber",
                table: "InvoiceLines",
                columns: new[] { "RectificativeInvoiceId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillingSource_RecipientTaxIdValue",
                table: "Invoices",
                columns: new[] { "BillingSource", "RecipientTaxIdValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillingSource_Serie_Year_SequenceNumber",
                table: "Invoices",
                columns: new[] { "BillingSource", "Serie", "Year", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssueDate",
                table: "Invoices",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSequences_BillingSource_Serie_Year",
                table: "InvoiceSequences",
                columns: new[] { "BillingSource", "Serie", "Year" },
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "InvoiceSequences");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "RectificativeInvoices");
        }
    }
}
