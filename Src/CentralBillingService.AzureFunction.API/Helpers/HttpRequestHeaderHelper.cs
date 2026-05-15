namespace CentralBillingService.AzureFunction.API.Helpers;

internal class HttpRequestHeaderHelper
{
    public static string GetSecret(HttpRequest req)
    {
        string secret = GetHeaderValue(req, "x-cbs-key");

        if (string.IsNullOrWhiteSpace(secret))
            throw new UnauthorizedAccessException("Invalid key");

        return secret;
    }

    private static string GetHeaderValue(HttpRequest req, string headerName)
    {
        string result = string.Empty;

        if (req != null && !string.IsNullOrWhiteSpace(headerName))
        {
            if (req.Headers.TryGetValue(headerName, out StringValues values))
            {
                string value = values.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    result = value;
                }
            }
        }

        return result;
    }
}
