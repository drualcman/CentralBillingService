namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Query parameters for listing invoices.
/// All filters are optional — omitting a field means no restriction on that dimension.
/// </summary>
public sealed class ListInvoicesQuery
{
    public string? Secret { get; init; }
    public string? BillingSource { get; init; }
    public string? Serie { get; init; }
    public int? Year { get; init; }
    public DateOnly? IssuedFrom { get; init; }
    public DateOnly? IssuedTo { get; init; }

    /// <summary>Filter by recipient tax ID — useful to find all invoices for a client.</summary>
    public string? RecipientTaxId { get; init; }

    /// <summary>Filter by the recipient's ID in the billing source's own system.</summary>
    public string? RecipientExternalId { get; init; }

    /// <summary>Filter by status: "Draft", "Issued", "Rectified", "Cancelled".</summary>
    public string? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}