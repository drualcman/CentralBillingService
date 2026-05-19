namespace CentralBillingService.Reports.Builders;

internal static class InvoiceReportSetupBuilder
{
    public static Setup Build()
    {
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
                Format = new Format(210, 122),
                Row = new Row { Dimension = new Dimension(210, (double)InvoiceReportLayout.BodyRowHeight) }
            },
            Footer = new Section { Format = new Format(210, (double)InvoiceReportLayout.FooterHeight) }
        };

        HeaderSectionBuilder.Build(setup.Header);
        BodySectionBuilder.Build(setup.Body);
        FooterSectionBuilder.Build(setup.Footer);

        return setup;
    }
}
