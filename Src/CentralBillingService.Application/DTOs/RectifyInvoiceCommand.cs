namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Input contract for the RectifyInvoice use case.
/// The caller provides the rectificative serie — the system manages
/// its own counter and hash chain for that serie independently.
/// </summary>
public sealed class RectifyInvoiceCommand
{
    /// <summary>
    /// Identifies which website or project is generating this invoice.
    /// Must match a registered billing source (e.g. "web-fotos", "web-cripto").
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// Identifies which website or project is generating this invoice.
    /// Must match a registered billing source (e.g. "web-fotos", "web-cripto").
    /// </summary>
    public required string BillingSource { get; init; }

    /// <summary>Number of the invoice to rectify (e.g. "FOTO2026-0003")</summary>
    public required string OriginalInvoiceNumber { get; init; }

    /// <summary>
    /// Serie the caller wants to use for the rectificative invoice.
    /// Tracked independently per BillingSource+Serie+Year.
    /// </summary>
    public required string RectificativeSerie { get; init; }

    public required string Reason { get; init; }
    public required ValueObjects.RectificationType RectificationType { get; init; }

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