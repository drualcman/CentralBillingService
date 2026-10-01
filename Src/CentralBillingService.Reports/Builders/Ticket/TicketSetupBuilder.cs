namespace CentralBillingService.Reports.Builders.Ticket;

using Columns = TicketReportLayout.Columns;

/// <summary>Page of the 80 mm ticket: centered QR and issuer, one two-line row per invoice line, totals below.</summary>
internal static class TicketSetupBuilder
{
    private const decimal FiscalQrX = (TicketReportLayout.PageWidth - TicketReportLayout.FiscalQrSize) / 2m;
    private const decimal HalfWidth = TicketReportLayout.ContentWidth / 2m;

    public static Setup Build(int lineCount)
    {
        decimal bodyHeight = TicketReportLayout.BodyHeight(lineCount);
        decimal pageHeight = TicketReportLayout.HeaderHeight + bodyHeight + TicketReportLayout.FooterHeight;
        double width = (double)TicketReportLayout.PageWidth;

        Setup setup = new Setup
        {
            Page = new Format
            {
                Orientation = Orientation.Portrait,
                Dimension = new Dimension(width, (double)pageHeight),
                Background = "White"
            },
            Header = new Section { Format = new Format(width, (double)TicketReportLayout.HeaderHeight) },
            Body = new Section
            {
                Format = new Format(width, (double)bodyHeight),
                Row = new Row { Dimension = new Dimension(width, (double)TicketReportLayout.BodyRowHeight) }
            },
            Footer = new Section { Format = new Format(width, (double)TicketReportLayout.FooterHeight) }
        };

        BuildHeader(setup.Header);
        BuildBody(setup.Body);
        BuildFooter(setup.Footer);
        return setup;
    }

    private static void BuildHeader(Section header)
    {
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)TicketReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(1, TicketReportLayout.Margin),
                Background = InvoiceReportLayout.TamperWarningColor,
                TextAlignment = TextAlignment.Center,
                FontDetails = new Font(new Shade(6.5, InvoiceReportLayout.WhiteText), new FontStyle(700))
            },
            DataColumn = new Item(Columns.TamperWarning)
        });
        header.AddColumn(TicketCellFormats.Image(Columns.Qr, 7, FiscalQrX, TicketReportLayout.FiscalQrSize));
        header.AddColumn(TicketCellFormats.Text(Columns.QrLegend, 37, 4, 8, bold: true,
            alignment: TextAlignment.Center, x: FiscalQrX, width: TicketReportLayout.FiscalQrSize));
        header.AddColumn(TicketCellFormats.Text(Columns.IssuerName, 42, 6, 11, bold: true, alignment: TextAlignment.Center));
        header.AddColumn(TicketCellFormats.Text(Columns.IssuerLegalName, 48, 4, 8, color: TicketReportLayout.GrayText, alignment: TextAlignment.Center));
        header.AddColumn(TicketCellFormats.Text(Columns.IssuerTaxId, 52, 4, 8, alignment: TextAlignment.Center));
        header.AddColumn(TicketCellFormats.Text(Columns.IssuerAddress, 56, 8, 8, alignment: TextAlignment.Center));
        header.AddColumn(TicketCellFormats.Separator(Columns.HeaderSeparator, 65));
        header.AddColumn(TicketCellFormats.Text(Columns.Title, 67, 5, 9, bold: true, width: HalfWidth + 12m));
        header.AddColumn(TicketCellFormats.Text(Columns.Date, 67.5m, 4, 8, alignment: TextAlignment.Right,
            x: TicketReportLayout.Margin + HalfWidth + 12m, width: HalfWidth - 12m));
        header.AddColumn(TicketCellFormats.Text(Columns.Number, 72, 4, 8));
        header.AddColumn(TicketCellFormats.Text(Columns.RecipientName, 77, 4, 8));
        header.AddColumn(TicketCellFormats.Text(Columns.RecipientTaxId, 81, 4, 8));
        header.AddColumn(TicketCellFormats.Separator(Columns.LinesSeparator, 87));
    }

    private static void BuildBody(Section body)
    {
        const string group = ReportCells.DetailGroup;
        const decimal totalWidth = 22m;
        const decimal descriptionWidth = TicketReportLayout.ContentWidth - totalWidth;

        body.AddColumn(TicketCellFormats.Text(Columns.LineDescription, 1, 5, 9, width: descriptionWidth, group: group));
        body.AddColumn(TicketCellFormats.Text(Columns.LineTotal, 1, 5, 9, bold: true, alignment: TextAlignment.Right,
            x: TicketReportLayout.Margin + descriptionWidth, width: totalWidth, group: group));
        body.AddColumn(TicketCellFormats.Text(Columns.LineDetail, 5.5m, 4, 7, color: TicketReportLayout.GrayText,
            x: TicketReportLayout.Margin + 2m, width: TicketReportLayout.ContentWidth - 2m, group: group));
    }

    private static void BuildFooter(Section footer)
    {
        const decimal valueWidth = 26m;
        const decimal valueX = TicketReportLayout.Margin + TicketReportLayout.ContentWidth - valueWidth;

        footer.AddColumn(TicketCellFormats.Separator(Columns.TotalsSeparator, 1));
        footer.AddColumn(TicketCellFormats.Text(Columns.BaseLabel, 3, 4, 8, width: 40));
        footer.AddColumn(TicketCellFormats.Text(Columns.BaseValue, 3, 4, 8, alignment: TextAlignment.Right, x: valueX, width: valueWidth));
        footer.AddColumn(TicketCellFormats.Text(Columns.TaxLabel, 8, 4, 8, width: 40));
        footer.AddColumn(TicketCellFormats.Text(Columns.TaxValue, 8, 4, 8, alignment: TextAlignment.Right, x: valueX, width: valueWidth));
        footer.AddColumn(TicketCellFormats.Text(Columns.TotalLabel, 13, 7, 12, bold: true, width: 40));
        footer.AddColumn(TicketCellFormats.Text(Columns.TotalValue, 13, 7, 12, bold: true, alignment: TextAlignment.Right, x: valueX, width: valueWidth));
        footer.AddColumn(TicketCellFormats.Separator(Columns.PaymentSeparator, 22));
        footer.AddColumn(TicketCellFormats.Text(Columns.Payment, 24, 4, 7.5));
        footer.AddColumn(TicketCellFormats.Text(Columns.PaymentReference, 28, 4, 7, color: TicketReportLayout.GrayText));
        footer.AddColumn(TicketCellFormats.Text(Columns.ExchangeRate, 32, 4, 7, color: TicketReportLayout.GrayText));
        footer.AddColumn(TicketCellFormats.Text(Columns.Notes, 36, 14, 7, color: TicketReportLayout.GrayText));
        footer.AddColumn(TicketCellFormats.Text(Columns.Origin, 51, 4, 7, color: TicketReportLayout.GrayText));
        footer.AddColumn(TicketCellFormats.Text(Columns.Hash, 55, 7, 6.5, color: "#555555"));
        footer.AddColumn(TicketCellFormats.Text(Columns.RectificationNotice, 63, 12, 7, bold: true));
    }
}
