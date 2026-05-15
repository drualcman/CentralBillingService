namespace CentralBillingService.Domain.Models;

public sealed class RecipientData
{
    public required string LegalName { get; init; }
    public string? TradeName { get; init; }
    public required string TaxIdValue { get; init; }
    public required string TaxIdCountryCode { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }

    // Dirección
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public string? Province { get; init; }
    public required string PostalCode { get; init; }
    public required string AddressCountryCode { get; init; }

    /// <summary>ID del receptor en el sistema del emisor (BusinessSource). Opcional.</summary>
    public string? ExternalId { get; init; }
}
