using DigitalDoor.Reporting.Entities.Models;
using DigitalDoor.Reporting.Entities.ValueObjects;

namespace CentralBillingService.Reports.Builders;

internal static class BodySectionBuilder
{
    private const double MainH = 8;    // altura de la fila principal en filas con sub-valor
    private const double SubH = 5;    // altura de la sub-fila de moneda original
    private const decimal SubY = 9m;  // desplazamiento Y de la sub-fila dentro de la celda

    public static void Build(Section body, bool hasOriginCurrency = false)
    {
        double cellH = hasOriginCurrency ? MainH : (double)(InvoiceReportLayout.BodyRowHeight - 2m);

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColDescW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColDescX + 3)
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.DescriptionValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColQtyW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColQtyX),
                TextAlignment = TextAlignment.Center
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.QtyValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColPriceW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColPriceX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.UnitPriceValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTaxW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColTaxX),
                TextAlignment = TextAlignment.Center
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TaxRateValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColBaseW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColBaseX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TaxableBaseValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTotalW, cellH)
            {
                Position = new Kernel(1, InvoiceReportLayout.ColTotalX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(10), new FontStyle(700))
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TotalValue)
        });

        if (!hasOriginCurrency) return;

        var originFont = new Font(new Shade(8, InvoiceReportLayout.GrayText));
        var originBoldFont = new Font(new Shade(8, InvoiceReportLayout.GrayText), new FontStyle(700));

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColPriceW, SubH)
            {
                Position = new Kernel(SubY, InvoiceReportLayout.ColPriceX),
                TextAlignment = TextAlignment.Right,
                FontDetails = originFont
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.UnitPriceOriginValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColBaseW, SubH)
            {
                Position = new Kernel(SubY, InvoiceReportLayout.ColBaseX),
                TextAlignment = TextAlignment.Right,
                FontDetails = originFont
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TaxableBaseOriginValue)
        });

        body.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTotalW, SubH)
            {
                Position = new Kernel(SubY, InvoiceReportLayout.ColTotalX),
                TextAlignment = TextAlignment.Right,
                FontDetails = originBoldFont
            },
            DataColumn = new Item("Detail", InvoiceReportLayout.Columns.TotalOriginValue)
        });
    }
}
