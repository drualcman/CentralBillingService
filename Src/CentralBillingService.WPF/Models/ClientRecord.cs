namespace CentralBillingService.WPF.Models;

public class ClientRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public string TaxId { get; set; } = "";
    public string TaxIdCountry { get; set; } = "ES";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; } = "ES";

    public string DisplayName => string.IsNullOrWhiteSpace(TradeName) ? LegalName : TradeName;
}
