namespace CentralBillingService.Infrastructure.Verification;

/// <summary>
/// Points QR codes at the Blazor verification UI.
///
/// URL format: {verifyUiBaseUrl}?invoiceNumber={n}&amp;hash={h}&amp;billingsource={bs}&amp;nif={taxId}&amp;fecha={date}&amp;importe={amount}
///
/// Same parameters as SpanishAeatVerificationUrlProvider (except invoiceNumber vs numserie
/// and the extra hash+billingsource needed by the CBS verify endpoint).
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
        string billingSource,
        string invoiceNumber,
        string hash,
        DateOnly issueDate,
        decimal totalEurAmount,
        string recipientTaxId) =>
        $"{_verifyUiBaseUrl}?invoiceNumber={Uri.EscapeDataString(invoiceNumber)}&hash={Uri.EscapeDataString(hash)}&billingsource={Uri.EscapeDataString(billingSource)}&recipienttaxid={Uri.EscapeDataString(recipientTaxId)}&issuedate={issueDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}&amount={totalEurAmount.ToString("F2", CultureInfo.InvariantCulture)}";
}
