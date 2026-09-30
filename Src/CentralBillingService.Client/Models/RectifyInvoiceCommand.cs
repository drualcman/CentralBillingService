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

    /// <summary>Issue date. Defaults to today (UTC) if not provided.</summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>
    /// Difference: required, the signed delta lines.
    /// Substitution: the corrected lines as the invoice should read; when omitted, the original's lines are copied.
    /// </summary>
    public IReadOnlyList<InvoiceLineDto>? Lines { get; init; }

    public string? Notes { get; init; }
    public string? PaymentMethod { get; init; }

    public required string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
}