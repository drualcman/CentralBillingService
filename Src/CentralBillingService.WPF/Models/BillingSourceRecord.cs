namespace CentralBillingService.WPF.Models;

public class BillingSourceRecord
{
    public string Key                { get; set; } = "";
    public string Secret             { get; set; } = "";
    public string LegalName          { get; set; } = "";
    public string? TradeName         { get; set; }
    public string TaxIdValue         { get; set; } = "";
    public string TaxIdCountryCode   { get; set; } = "ES";
    public string Email              { get; set; } = "";
    public string? Phone             { get; set; }
    public string? Website           { get; set; }
    public string AddressLine1       { get; set; } = "";
    public string City               { get; set; } = "";
    public string PostalCode         { get; set; } = "";
    public string AddressCountryCode { get; set; } = "ES";

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(TradeName) ? TradeName : LegalName;
}
