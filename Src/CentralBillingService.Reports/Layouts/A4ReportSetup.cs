namespace CentralBillingService.Reports.Layouts;

/// <summary>Page setup shared by the A4 models.</summary>
internal static class A4ReportSetup
{
    // Wide rows when the invoice OR any individual line has a non-EUR currency: a mixed-currency
    // invoice may leave IsInOriginCurrency=false while lines still need room for their origin values.
    public static Setup For(Invoice invoice) =>
        InvoiceReportSetupBuilder.Build(invoice.IsInOriginCurrency || invoice.Lines.Any(line => line.HasCurrencyConversion));
}
