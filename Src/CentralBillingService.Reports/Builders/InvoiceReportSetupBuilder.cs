namespace CentralBillingService.Reports.Builders;

internal static class InvoiceReportSetupBuilder
{
    public static Setup Build(bool hasOriginCurrency = false)
    {
        double rowHeight = hasOriginCurrency
            ? (double)InvoiceReportLayout.BodyRowHeightWithOrigin
            : (double)InvoiceReportLayout.BodyRowHeight;

        var setup = new Setup
        {
            Page = new Format
            {
                Orientation = Orientation.Portrait,
                Dimension = PageSize.A4,
                Background = "White"
            },
            Header = new Section { Format = new Format(210, (double)InvoiceReportLayout.HeaderHeight) },
            Body = new Section
            {
                Format = new Format(210, (double)InvoiceReportLayout.BodyHeight),
                Row = new Row { Dimension = new Dimension(210, rowHeight) }
            },
            Footer = new Section { Format = new Format(210, (double)InvoiceReportLayout.FooterHeight) }
        };

        HeaderSectionBuilder.Build(setup.Header);
        BodySectionBuilder.Build(setup.Body, hasOriginCurrency);
        FooterSectionBuilder.Build(setup.Footer);

        return setup;
    }
}
