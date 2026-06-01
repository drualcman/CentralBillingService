namespace CentralBillingService.Application.UseCases;

public class GenerateInvoiceUseCase(
    GenerateInvoiceReportUseCase generateInvoiceReport,
    IReportAsBytes reportAsBytes,
    IBlobStorageService invoiceStorageService,
    BillingSourceRegistry registry,
    IHttpClientFactory clientFactory,
    GetInvoiceUseCase getInvoiceUseCase,
    IJobQueue jobQueue,
    ILogger<GenerateInvoiceUseCase> logger,
    IIso9001 iso9001)
{
    public async Task GenerateInvoice(GenerateInvoiceReportCommand data)
    {
        await iso9001.Register(data.InvoiceNumber, this, "Generating PDF Report", data);
        var report = await generateInvoiceReport.GenerateInvoiceViewModel(data, CancellationToken.None);
        byte[] invoiceBytes = await reportAsBytes.GenerateReport(report);
        await invoiceStorageService.UploadInvoiceAsync(
            InvoiceHelper.GetInvoiceFileName(data.BillingSource, data.InvoiceNumber), invoiceBytes);
        await iso9001.Register(data.InvoiceNumber, this, "PDF Report generated and uploaded");

        var configuration = registry.GetConfig(data.BillingSource);

        if (configuration is not null)
        {
            InvoiceResult invoice = null;
            if ((configuration.Callback is not null && !string.IsNullOrEmpty(configuration.Callback.Url)) ||
                (configuration.ResultQueue is not null && !string.IsNullOrEmpty(configuration.ResultQueue.ConnectionString)))
            {
                invoice = await getInvoiceUseCase.ExecuteAsync(new GetInvoiceQuery
                {
                    BillingSource = data.BillingSource,
                    InvoiceNumber = data.InvoiceNumber,
                    Secret = configuration.Secret
                }, CancellationToken.None);
            }

            if (invoice is not null)
            {
                if (configuration.Callback is not null && !string.IsNullOrEmpty(configuration.Callback.Url))
                {
                    using HttpClient client = clientFactory.CreateClient();
                    if (!string.IsNullOrWhiteSpace(configuration.Callback.AuthHeader))
                        client.DefaultRequestHeaders
                            .TryAddWithoutValidation(configuration.Callback.AuthHeader, configuration.Callback.AuthToken);
                    string url = $"{configuration.Callback.Url}?userId={Uri.EscapeDataString(invoice.Recipient.ExternalId)}&invoiceNumber={Uri.EscapeDataString(data.InvoiceNumber)}";

                    await iso9001.Register(data.InvoiceNumber, this, $"Callback to {url}");
                    try
                    {
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex.Message);
                        await iso9001.Error(data.InvoiceNumber, this, $"Callback to {url}", ex);
                    }
                }
                if (configuration.ResultQueue is not null && !string.IsNullOrEmpty(configuration.ResultQueue.ConnectionString))
                {
                    var message = new
                    {
                        number = data.InvoiceNumber,
                        name = invoice.Recipient.DisplayName,
                        email = invoice.Recipient.Email,
                        custorId = invoice.Recipient.ExternalId,
                        euroAmount = invoice.TotalEur,
                        currencyAmount = invoice.TotalInOriginCurrency.Amount,
                        currency = invoice.TotalInOriginCurrency.CurrencyCode,
                        exchangeRate = invoice.AppliedExchangeRate.Rate
                    };
                    string json = JsonSerializer.Serialize(message, JsonOptions.Default);
                    await iso9001.Register(data.InvoiceNumber, this, $"Enqueue to {configuration.ResultQueue.QueueName}");
                    await jobQueue.EnqueueAsync(configuration.ResultQueue.ConnectionString,
                        configuration.ResultQueue.QueueName, "", CancellationToken.None);
                }
            }
        }
    }
}
