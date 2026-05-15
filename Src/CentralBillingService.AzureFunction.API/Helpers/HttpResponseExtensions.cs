namespace CentralBillingService.AzureFunction.API.Helpers;

/// <summary>
/// Extension methods for building consistent HTTP error responses
/// following RFC 7807 (Problem Details for HTTP APIs).
///
/// WriteAsJsonAsync is not available on HttpResponseData in the isolated worker model.
/// We serialize manually and write to the response body stream instead.
/// </summary>
internal static class HttpResponseExtensions
{
    internal static async Task<HttpResponseData> CreateProblemResponseAsync(
        this HttpRequestData req,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/problem+json; charset=utf-8");

        var body = JsonSerializer.Serialize(new ProblemDetail
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
        }, JsonOptions.Default);

        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(body));
        return response;
    }

    internal static async Task WriteJsonAsync<T>(
        this HttpResponseData response,
        T value,
        CancellationToken cancellationToken = default)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var body = JsonSerializer.Serialize(value, JsonOptions.Default);
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(body), cancellationToken);
    }
}

internal sealed class ProblemDetail
{
    public int Status { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}