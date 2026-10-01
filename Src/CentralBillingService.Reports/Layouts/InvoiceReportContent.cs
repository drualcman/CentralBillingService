namespace CentralBillingService.Reports.Layouts;

/// <summary>Everything a printed model needs: the document plus the facts computed around it.</summary>
/// <param name="HasTamper">Outcome of the document's own integrity check; prints the warning banner.</param>
/// <param name="RectificationNotice">For a rectificative: its condition, the rectified invoice and the reason
/// (mandatory on a corrective invoice, art. 15 RD 1619/2012). Null for an ordinary invoice.</param>
public sealed record InvoiceReportContent(Invoice Invoice, bool HasTamper, string LogoUrl, string? RectificationNotice);
