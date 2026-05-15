namespace CentralBillingService.Client.Models;

public sealed class GetInvoicesQuery
{
    public string? Serie { get; init; }
    public int? Year { get; init; }
    public DateOnly? IssuedFrom { get; init; }
    public DateOnly? IssuedTo { get; init; }
    public string? RecipientTaxId { get; init; }
    public string? RecipientExternalId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
