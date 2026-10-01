namespace CentralBillingService.Reports.Layouts;

/// <summary>
/// The printed models CBS offers, by name. An unknown name is printed with the classic invoice.
/// </summary>
internal static class InvoiceReportLayouts
{
    private static readonly IInvoiceReportLayout Classic = new ClassicInvoiceReportLayout();

    private static readonly IReadOnlyDictionary<string, IInvoiceReportLayout> ByName =
        new IInvoiceReportLayout[]
        {
            Classic,
            new TicketInvoiceReportLayout(),
            new SubscriptionInvoiceReportLayout(),
        }.ToDictionary(layout => layout.Name, StringComparer.OrdinalIgnoreCase);

    public static IInvoiceReportLayout For(string layoutName) =>
        ByName.TryGetValue(layoutName, out IInvoiceReportLayout? layout) ? layout : Classic;
}
