namespace CentralBillingService.AzureFunction.API.Helpers;

internal static class HttpRequestBodyHelper
{
    public static async Task<TValue> GetRequestedModel<TValue>(HttpRequestData req,
        CancellationToken cancellationToken) where TValue : class
    {
        TValue data = await JsonSerializer.DeserializeAsync<TValue>(req.Body,
            JsonOptions.Default, cancellationToken)
        ?? throw new ArgumentException("Request body is empty or invalid.");
        return data;
    }
}