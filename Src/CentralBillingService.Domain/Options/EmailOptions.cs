namespace CentralBillingService.Domain.Options;

public class EmailOptions
{
    public const string SectionKey = nameof(EmailOptions);
    public string Url { get; set; } = string.Empty;
    public int CompanyId { get; set; } = 3;
}
