namespace CentralBillingService.Domain.DTOs;

public sealed class RectifyInvoiceRequest
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

    /// <summary>
    /// Serie de factura para este origen.
    /// Ej: "F" para la serie general, "FOTO", "TV", "CRIPTO", "REC" para rectificativas.
    /// </summary>
    public required string RectificativeSerie { get; init; }
    public required string Reason { get; init; }
    public required RectificationType RectificationType { get; init; }

    /// <summary>Issue date. Defaults to today (UTC) if not provided.</summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>
    /// Solo requerido para RectificationType.Difference.
    /// En Substitution se calculan automáticamente desde la original.
    /// </summary>
    public IReadOnlyList<InvoiceLineData>? Lines { get; init; }

    public string? Notes { get; init; }

    public string? PaymentMethod { get; init; }

    public required string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
}
