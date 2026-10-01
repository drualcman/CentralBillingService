namespace CentralBillingService.Reports.Layouts;

/// <summary>
/// One printed model of an invoice, selected by <see cref="Name"/> (see InvoiceLayoutNames). Every model
/// must print the legal blocks: fiscal QR with its legend, issuer, number and date, tax breakdown,
/// verification and, for a rectificative, its notice.
/// </summary>
internal interface IInvoiceReportLayout
{
    string Name { get; }

    Task<ReportViewModel> BuildAsync(InvoiceReportContent content);
}
