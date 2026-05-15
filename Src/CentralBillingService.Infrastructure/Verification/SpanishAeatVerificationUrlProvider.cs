namespace CentralBillingService.Infrastructure.Verification;

/// <summary>
/// Returns the Spanish Tax Agency (AEAT) verification URL for VeriFactu compliance
/// as required by Real Decreto 1007/2023.
///
/// Use this provider when the system is certified under VeriFactu and invoices
/// must be verifiable directly against the AEAT registry.
///
/// Register this instead of SystemInvoiceVerificationUrlProvider once VeriFactu
/// submission is fully implemented.
/// </summary>
public sealed class SpanishAeatVerificationUrlProvider : IInvoiceVerificationUrlProvider
{
    public string GetVerificationUrl(
        string invoiceNumber,
        string hash,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId) =>
        "https://www2.agenciatributaria.gob.es/wlpl/VFPR-CONT/VFPRValidarQR" +
        $"?nif={Uri.EscapeDataString(issuerTaxId)}" +
        $"&numserie={Uri.EscapeDataString(invoiceNumber)}" +
        $"&fecha={issueDate:dd-MM-yyyy}" +
        $"&importe={totalEurAmount:F2}";
}
