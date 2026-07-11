namespace CentralBillingService.Client.Options;

public class CbsOptions
{
    public const string SectionKey = nameof(CbsOptions);
    public string Uri { get; set; } = string.Empty;
    public string BillingSource { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// HTTP request timeout in seconds for calls to CBS.
    /// Defaults to 180s (3 min): invoice creation runs off a queue with no user waiting,
    /// so a slow cold start or a throttled database in production should not abort the
    /// request. An aborted request cancels the CBS-side SaveChanges (OperationCanceledException)
    /// and risks a burned sequence number, so we prefer to wait.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 200;
}
