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

    /// <summary>
    /// Base URL of the Blazor verification UI (e.g. "https://cbs-verify.azurestaticapps.net").
    /// QR codes point here so recipients see a human-readable verification page.
    /// </summary>
    public string VerifyUiBaseUrl { get; set; } = string.Empty;

    /// <summary>Connection string to the Azure Storage account used for QR code blobs.</summary>
    public string QrBlobConnectionString { get; set; } = string.Empty;

    /// <summary>Blob container name where QR code PNGs are stored.</summary>
    public string QrBlobContainerName { get; set; } = "cbs-qr-codes";

    /// <summary>Azure Storage Queue name where QR generation jobs are sent.</summary>
    public string QrCodeQueueName { get; set; } = "cbs-qr-code-jobs";
}
