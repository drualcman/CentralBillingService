namespace CentralBillingService.Domain.Models;

public sealed class Customer
{
    public string Name { get; }

    public string TaxId { get; }

    public string Email { get; }

    public string Address { get; }

    public string CountryCode { get; }

    public Customer(
        string name,
        string taxId,
        string email,
        string address,
        string countryCode)
    {
        Name = name?.Trim() ?? string.Empty;

        TaxId = taxId?.Trim().ToUpperInvariant() ?? string.Empty;

        Email = email?.Trim() ?? string.Empty;

        Address = address?.Trim() ?? string.Empty;

        CountryCode = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
