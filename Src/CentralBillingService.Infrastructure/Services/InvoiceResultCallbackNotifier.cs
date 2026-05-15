namespace CentralBillingService.Infrastructure.Services;

public sealed class InvoiceResultCallbackNotifier : IInvoiceResultCallbackNotifier
{
    private readonly HttpClient _httpClient;

    public InvoiceResultCallbackNotifier(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task NotifyAsync(
        InvoiceResult result,
        CallbackConfig config,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, config.Url);

        if (!string.IsNullOrWhiteSpace(config.AuthHeader) &&
            !string.IsNullOrWhiteSpace(config.AuthToken))
            request.Headers.TryAddWithoutValidation(config.AuthHeader, config.AuthToken);

        request.Content = JsonContent.Create(result);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
