namespace CentralBillingService.Domain.Options;

public class CbsOptions
{
    public const string SectionKey = nameof(CbsOptions);

    public BillingSourceConfig[] BillingSources { get; set; }

    /// <summary>
    /// Base URL of this system's API (e.g. "https://billing.mycompany.com").
    /// Used to build the QR verification URL printed on invoice PDFs.
    /// </summary>
    public string SystemBaseUrl { get; set; } = string.Empty;
}
