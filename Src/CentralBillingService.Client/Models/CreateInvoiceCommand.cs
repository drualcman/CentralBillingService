namespace CentralBillingService.Client.Models;

/// <summary>
/// Input contract for the CreateInvoice use case.
/// This is what the Azure Function, a desktop app, or a test sends in.
/// Deliberately flat and primitive — no domain types leak out of Application.
/// </summary>
public sealed class CreateInvoiceCommand
{
    /// <summary>
    /// Serie de factura para este origen.
    /// Ej: "F" para la serie general, "FOTO", "TV", "CRIPTO", "REC" para rectificativas.
    /// </summary>
    public required string Serie { get; init; }

    public required RecipientDto Recipient { get; init; }
    public required IReadOnlyList<InvoiceLineDto> Lines { get; init; }

    /// <summary>
    /// Currency code in which the client sees the amounts (e.g. "USD", "PHP", "EUR").
    /// If EUR, no conversion is performed.
    /// </summary>
    public required string OriginCurrencyCode { get; init; }

    /// <summary>Issue date. Defaults to today (UTC) if not provided.</summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>Value date if different from issue date (e.g. subscription period).</summary>
    public DateOnly? ValueDate { get; init; }

    public string? Notes { get; init; }

    public required string PaymentMethod { get; init; }
    public required string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
}