namespace CentralBillingService.Domain.Models;

public sealed class CallbackConfig
{
    public required string Url { get; init; }

    /// <summary>HTTP header name for auth (e.g. "Authorization", "X-Api-Key").</summary>
    public string? AuthHeader { get; init; }

    /// <summary>Value for the auth header.</summary>
    public string? AuthToken { get; init; }
}
