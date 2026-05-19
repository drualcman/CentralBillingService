namespace CentralBillingService.Reports.Builders;

internal static class DownloadUrlHelper
{
    public static async ValueTask<byte[]> GetBytes(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}
