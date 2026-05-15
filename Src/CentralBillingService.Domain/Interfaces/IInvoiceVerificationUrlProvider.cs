namespace CentralBillingService.Domain.Interfaces;

/// <summary>
/// Provides a URL that a recipient can use to independently verify an invoice's
/// authenticity and integrity.
///
/// The default implementation points to this system's own verification endpoint.
/// Other implementations can target external fiscal authorities (e.g., Spain's AEAT
/// for VeriFactu compliance, or equivalent bodies in other jurisdictions).
///
/// Register the appropriate implementation in the DI container for your deployment.
/// </summary>
public interface IInvoiceVerificationUrlProvider
{
    /// <summary>
    /// Returns the verification URL for the given invoice data.
    /// This URL is encoded in the QR code printed on the invoice PDF so the
    /// recipient can verify authenticity. The hash is included so the verifier
    /// can confirm the specific document version matches what was issued.
    /// </summary>
    string GetVerificationUrl(
        string invoiceNumber,
        string hash,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId);
}
