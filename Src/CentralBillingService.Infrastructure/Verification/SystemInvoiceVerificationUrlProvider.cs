namespace CentralBillingService.Infrastructure.Verification;

/// <summary>
/// Points QR codes at the Blazor verification UI with the invoice number and hash
/// as query parameters. The UI then calls the CBS API to display the verification result.
///
/// URL format: {verifyUiBaseUrl}?invoiceNumber={number}&amp;hash={hash}
///
/// Swap for SpanishAeatVerificationUrlProvider when VeriFactu submission is live.
/// </summary>
public sealed class SystemInvoiceVerificationUrlProvider : IInvoiceVerificationUrlProvider
{
    private readonly string _verifyUiBaseUrl;

    public SystemInvoiceVerificationUrlProvider(string verifyUiBaseUrl)
    {
        _verifyUiBaseUrl = verifyUiBaseUrl.TrimEnd('/');
    }

    public string GetVerificationUrl(
        string invoiceNumber,
        string hash,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId) =>
        $"{_verifyUiBaseUrl}?invoiceNumber={Uri.EscapeDataString(invoiceNumber)}&hash={Uri.EscapeDataString(hash)}";
}
