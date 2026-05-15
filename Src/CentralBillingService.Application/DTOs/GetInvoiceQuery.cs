namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Query to retrieve a single invoice by number or ID.
/// Only one of the two fields needs to be provided.
/// </summary>
public sealed class GetInvoiceQuery
{
    public string? BillingSource { get; init; }
    public string? Secret { get; init; }

    /// <summary>Human-readable invoice number, e.g. "FOTO2026-0003"</summary>
    public string? InvoiceNumber { get; init; }

    /// <summary>Internal UUID — preferred when available, faster lookup.</summary>
    public Guid? Id { get; init; }
}
