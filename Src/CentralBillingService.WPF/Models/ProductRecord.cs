namespace CentralBillingService.WPF.Models;

public class ProductRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal DefaultUnitPrice { get; set; }
    public decimal DefaultTaxRate { get; set; } = 21;
    public string? DefaultCurrencyCode { get; set; }
}
