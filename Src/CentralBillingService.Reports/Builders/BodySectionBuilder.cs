using DigitalDoor.Reporting.Entities.Models;
using DigitalDoor.Reporting.Entities.ValueObjects;

namespace CentralBillingService.Reports.Builders;

internal static class BodySectionBuilder
{
    // Row height - 2mm padding = content height per cell
    private const double CellH = (double)(InvoiceReportLayout.BodyRowHeight - 2m);

    public static void Build(Section body)
    {
        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColDescW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColDescX + 3)
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.DescriptionValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColQtyW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColQtyX),
                TextAlignment = TextAlignment.Center
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.QtyValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColPriceW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColPriceX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.UnitPriceValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTaxW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColTaxX),
                TextAlignment = TextAlignment.Center
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TaxRateValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColBaseW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColBaseX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TaxableBaseValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTotalW, CellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColTotalX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(10), new FontStyle(700))
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TotalValue)
        });
    }
}
