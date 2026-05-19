namespace CentralBillingService.Domain.Models;

/// <summary>
/// Flat, bindable DTO for issuer configuration. Mirrors the JSON structure in appsettings.
/// Converts to the BillingParty domain entity via ToBillingParty().
/// </summary>
public class IssuerConfig
{
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public string TaxIdValue { get; set; } = "";
    public string TaxIdCountryCode { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string AddressLine1 { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = "";
    public string? Province { get; set; }
    public string PostalCode { get; set; } = "";
    public string AddressCountryCode { get; set; } = "";
    public string? LogoUrl { get; set; }

    public BillingParty ToBillingParty()
    {
        var taxId = TaxId.Create(TaxIdValue, TaxIdCountryCode);
        var address = PostalAddress.Create(AddressLine1, City, PostalCode, AddressCountryCode, AddressLine2, Province);
        return BillingParty.Create(LegalName, taxId, address, Email, TradeName, Phone, Website);
    }

    public static IssuerConfig From(BillingParty party) => new()
    {
        LegalName = party.LegalName,
        TradeName = party.TradeName,
        TaxIdValue = party.TaxId.Value,
        TaxIdCountryCode = party.TaxId.CountryCode,
        Email = party.Email,
        Phone = party.Phone,
        Website = party.Website,
        AddressLine1 = party.Address.Line1,
        AddressLine2 = party.Address.Line2,
        City = party.Address.City,
        Province = party.Address.Province,
        PostalCode = party.Address.PostalCode,
        AddressCountryCode = party.Address.CountryCode,
    };
}
