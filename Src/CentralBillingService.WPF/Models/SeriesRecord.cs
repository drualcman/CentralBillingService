namespace CentralBillingService.WPF.Models;

public class SeriesRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Rectificatives must be issued in their own specific series (RD 1619/2012 art. 6.1.a).</summary>
    public bool IsRectificative { get; set; }
}
