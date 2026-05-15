namespace CentralBillingService.Application.Models;

/// <summary>
/// Filter parameters for invoice listing queries.
/// All fields are optional — omitting a field means no filter on that dimension.
/// </summary>
public sealed class InvoiceFilter
{
    public string? BillingSource { get; init; }
    public string? Serie { get; init; }
    public int? Year { get; init; }
    public DateOnly? IssuedFrom { get; init; }
    public DateOnly? IssuedTo { get; init; }
    public string? RecipientTaxId { get; init; }

    /// <summary>ID del receptor en el sistema del emisor (BusinessSource). Permite filtrar por cliente sin conocer el NIF.</summary>
    public string? RecipientExternalId { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
