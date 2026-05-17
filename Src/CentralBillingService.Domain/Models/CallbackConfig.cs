namespace CentralBillingService.Domain.Models;

public sealed class CallbackConfig
{
    public string Url { get; set; } = "";

    /// <summary>HTTP header name for auth (e.g. "Authorization", "X-Api-Key").</summary>
    public string? AuthHeader { get; set; }

    /// <summary>Value for the auth header.</summary>
    public string? AuthToken { get; set; }
}
