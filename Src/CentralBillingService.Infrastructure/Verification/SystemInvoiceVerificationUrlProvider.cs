namespace CentralBillingService.Infrastructure.Verification;

/// <summary>
/// Returns the URL of this system's own invoice verification endpoint:
///   GET {baseUrl}/api/invoices/{invoiceNumber}/verify
///
/// This is the default provider. The QR code on invoice PDFs points to this URL
/// so recipients can verify the invoice directly against this system's records.
/// </summary>
public sealed class SystemInvoiceVerificationUrlProvider : IInvoiceVerificationUrlProvider
{
    private readonly string _baseUrl;

    public SystemInvoiceVerificationUrlProvider(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public string GetVerificationUrl(
        string invoiceNumber,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId) =>
        $"{_baseUrl}/api/invoices/{Uri.EscapeDataString(invoiceNumber)}/verify";
}
