namespace CentralBillingService.Client.Options;

public class CbsOptions
{
    public const string SectionKey = nameof(CbsOptions);
    public string Uri { get; set; } = string.Empty;
    public string BillingSource { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}
