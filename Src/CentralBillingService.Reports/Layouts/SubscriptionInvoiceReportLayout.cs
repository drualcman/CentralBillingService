namespace CentralBillingService.Reports.Layouts;

/// <summary>
/// A4 invoice for subscriptions: no quantity nor unit price, a wide description and one amount per line
/// (tax included). Base and tax appear only in the totals footer.
/// </summary>
internal sealed class SubscriptionInvoiceReportLayout : IInvoiceReportLayout
{
    public string Name => InvoiceLayoutNames.Subscription;

    public async Task<ReportViewModel> BuildAsync(InvoiceReportContent content)
    {
        List<ColumnData> data = await InvoiceDataBuilder.BuildAsync(content, InvoiceTableHeaders.Subscriptions, AddRows);
        return new ReportViewModel(A4ReportSetup.For(content.Invoice), data);
    }

    private static void AddRows(List<ColumnData> data, Invoice invoice)
    {
        if (invoice.Lines.Count == 0)
            LineRowsDataBuilder.AddEmptyRow(data);
        else
            LineRowsDataBuilder.AddSubscriptionRows(data, invoice.Lines, firstRow: 1);
    }
}
