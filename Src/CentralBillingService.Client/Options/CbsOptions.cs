namespace CentralBillingService.Client.Options;

public class CbsOptions
{
    public static string SectionKey = nameof(CbsOptions);
    public string Uri { get; set; }
    public string BillingSource { get; set; }
    public string AppKey { get; set; }
    public string AppSecret { get; set; }
}
