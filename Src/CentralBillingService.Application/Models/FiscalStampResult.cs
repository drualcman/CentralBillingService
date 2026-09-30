namespace CentralBillingService.Application.Models;

/// <summary>
/// What a fiscal registrar produces synchronously when an invoice is created:
/// the authority-conformant fingerprint (e.g. VeriFactu "huella") to print on the
/// invoice, and the content to encode in its verification QR code.
/// A registrar with no fiscal registration returns <see cref="None"/>.
/// </summary>
public sealed record FiscalStampResult(string? Huella, string? QrContent)
{
    public static readonly FiscalStampResult None = new(null, null);

    /// <summary>True when the registrar produced a fiscal fingerprint to attach.</summary>
    public bool HasStamp => !string.IsNullOrWhiteSpace(Huella);
}
