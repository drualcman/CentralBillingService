namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result returned after a successful rectification.
/// Contains both the updated original and the new rectificative invoice.
/// </summary>
public sealed class RectifyInvoiceResult
{
    /// <summary>The original invoice, now in Rectified status.</summary>
    public required InvoiceResult UpdatedOriginal { get; init; }

    /// <summary>The new rectificative invoice, in Issued status.</summary>
    public required RectificativeInvoiceResult Rectificative { get; init; }
}
