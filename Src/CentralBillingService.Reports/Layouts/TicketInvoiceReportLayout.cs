namespace CentralBillingService.Reports.Layouts;

/// <summary>Reduced invoice on an 80 mm thermal roll, as long as its content.</summary>
internal sealed class TicketInvoiceReportLayout : IInvoiceReportLayout
{
    public string Name => InvoiceLayoutNames.Ticket;

    public async Task<ReportViewModel> BuildAsync(InvoiceReportContent content)
    {
        Setup setup = TicketSetupBuilder.Build(content.Invoice.Lines.Count);
        List<ColumnData> data = await TicketDataBuilder.BuildAsync(content);
        return new ReportViewModel(setup, data);
    }
}
