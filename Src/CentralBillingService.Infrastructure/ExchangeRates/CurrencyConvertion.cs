namespace CentralBillingService.Infrastructure.ExchangeRates;

public class CurrencyConvertion(HttpClient Client) : ICurrencyConvertion
{
    public async Task<decimal> GetRate(Currency origin, Currency destination)
    {
        if (origin != destination)
        {
            return await GetCurrentRate(origin.ToString(), destination.ToString());
        }
        else
            return 1m;
    }
    public async Task<Money> ConvertToCurrency(Money origin, Currency destination)
    {
        if (origin.Currency != destination)
        {
            decimal rate = await GetCurrentRate(origin.Currency.ToString(), destination.ToString());
            return Money.Of(origin.Amount * rate, destination);
        }
        else
            return origin;
    }

    async Task<decimal> GetCurrentRate(string baseCurrency, string targetCurrency)
    {
        decimal result;
        string apiUrl = $"{baseCurrency}";

        try
        {
            HttpResponseMessage response = await Client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            ExchangeRateData jsonData = JsonSerializer.Deserialize<ExchangeRateData>(responseBody, options);

            DateTime date = Convert.ToDateTime(jsonData.TimeLastUpdateUtc);
            result = jsonData.Rates[targetCurrency];
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            result = 1.0m;
        }

        return result;
    }

    class ExchangeRateData
    {
        [JsonPropertyName("base_code")]
        public string Base { get; set; }
        public string Result { get; set; }
        public string Provider { get; set; }
        public string Documentation { get; set; }
        [JsonPropertyName("terms_of_use ")]
        public string TermsOfUse { get; set; }
        [JsonPropertyName("time_last_update_unix")]
        public int TimeLastUpdateUnix { get; set; }
        [JsonPropertyName("time_last_update_utc")]
        public string TimeLastUpdateUtc { get; set; }
        [JsonPropertyName("time_next_update_unix")]
        public int TimeNextUpdateUnix { get; set; }
        [JsonPropertyName("time_next_update_utc")]
        public string TimeNextUpdateUtc { get; set; }
        [JsonPropertyName("time_eol_unix")]
        public int TimeEolUnix { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

}
