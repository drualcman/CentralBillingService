namespace CentralBillingService.WPF.Models;

public class SeriesRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string? Description { get; set; }
}
