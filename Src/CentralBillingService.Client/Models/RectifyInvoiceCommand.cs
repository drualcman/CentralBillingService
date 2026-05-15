namespace CentralBillingService.Client.Models;

/// <summary>
/// Input contract for the RectifyInvoice use case.
/// The caller provides the rectificative serie — the system manages
/// its own counter and hash chain for that serie independently.
/// </summary>
public sealed class RectifyInvoiceCommand
{
    /// <summary>
    /// Serie the caller wants to use for the rectificative invoice.
    /// Tracked independently per BillingSource+Serie+Year.
    /// </summary>
    public required string RectificativeSerie { get; init; }

    public required string Reason { get; init; }
    public required RectificationType RectificationType { get; init; }

    /// <summary>
    /// Required only for Difference rectifications.
    /// For Substitution, lines are derived automatically from the original.
    /// </summary>
    public IReadOnlyList<InvoiceLineDto>? Lines { get; init; }

    public string? Notes { get; init; }
    public string? PaymentMethod { get; init; }

    public required string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
}