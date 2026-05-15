namespace CentralBillingService.Client.HttpClients;

internal class CbsHttpClient(
    HttpClient client,
    IOptions<CbsOptions> options) : ICbsService
{
    public async Task<InvoiceCreateReult> CreateInvoiceAsync(CreateInvoiceCommand invoiceData)
    {
        var response = await client.PostAsJsonAsync("invoices", invoiceData);
        response.EnsureSuccessStatusCode();
        InvoiceResult result = await response.Content.ReadFromJsonAsync<InvoiceResult>();
        return new InvoiceCreateReult()
        {
            InvoiceNumber = result.InvoiceNumber,
            Hash = result.Hash,
            Status = result.Status
        };
    }

    public async Task<RectifyInvoiceResult> RectifyInvoiceAsync(string invoiceNumber, RectifyInvoiceCommand invoiceData)
    {
        var response = await client.PostAsJsonAsync($"invoices/{invoiceNumber}/rectify", invoiceData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RectifyInvoiceResult>();
    }

    public async Task<InvoiceResult> GetInvoiceAsync(string invoiceNumber)
    {
        var response = await client.GetAsync($"invoices/{invoiceNumber}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvoiceResult>();
    }

    public async Task<InvoiceListResult> GetInvoicesAsync(GetInvoicesQuery? filter = null)
    {
        var url = BuildInvoicesUrl(filter);
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvoiceListResult>();
    }

    public async Task<VerifyInvoiceResult> VerifyInvoiceAsync(string invoiceNumber, string documentHash)
    {
        var response = await client.GetAsync($"invoices/{invoiceNumber}/verify?hash={Uri.EscapeDataString(documentHash)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VerifyInvoiceResult>();
    }

    private static string BuildInvoicesUrl(GetInvoicesQuery? filter)
    {
        if (filter is null) return "invoices";

        var parameters = new List<string>();

        if (filter.Serie is not null)
            parameters.Add($"serie={Uri.EscapeDataString(filter.Serie)}");
        if (filter.Year.HasValue)
            parameters.Add($"year={filter.Year}");
        if (filter.IssuedFrom.HasValue)
            parameters.Add($"issuedFrom={filter.IssuedFrom.Value:yyyy-MM-dd}");
        if (filter.IssuedTo.HasValue)
            parameters.Add($"issuedTo={filter.IssuedTo.Value:yyyy-MM-dd}");
        if (filter.RecipientTaxId is not null)
            parameters.Add($"recipientTaxId={Uri.EscapeDataString(filter.RecipientTaxId)}");
        if (filter.RecipientExternalId is not null)
            parameters.Add($"recipientExternalId={Uri.EscapeDataString(filter.RecipientExternalId)}");
        if (filter.Status is not null)
            parameters.Add($"status={Uri.EscapeDataString(filter.Status)}");
        if (filter.Page != 1)
            parameters.Add($"page={filter.Page}");
        if (filter.PageSize != 25)
            parameters.Add($"pageSize={filter.PageSize}");

        return parameters.Count == 0 ? "invoices" : $"invoices?{string.Join("&", parameters)}";
    }
}
