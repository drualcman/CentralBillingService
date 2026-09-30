using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralBillingService.VeriFactu.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionChainSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChainSequence",
                table: "VeriFactuSubmission",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Backfill: number existing records of each (NIF, billing source) chain in stamping order.
            migrationBuilder.Sql(@"
                WITH Ordered AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY IssuerNif, BillingSource ORDER BY CreatedUtc) AS Seq
                    FROM VeriFactuSubmission)
                UPDATE s SET ChainSequence = o.Seq
                FROM VeriFactuSubmission s JOIN Ordered o ON o.Id = s.Id;");

            migrationBuilder.CreateIndex(
                name: "IX_VeriFactuSubmission_IssuerNif_BillingSource_ChainSequence",
                table: "VeriFactuSubmission",
                columns: new[] { "IssuerNif", "BillingSource", "ChainSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VeriFactuSubmission_IssuerNif_BillingSource_ChainSequence",
                table: "VeriFactuSubmission");

            migrationBuilder.DropColumn(
                name: "ChainSequence",
                table: "VeriFactuSubmission");
        }
    }
}
