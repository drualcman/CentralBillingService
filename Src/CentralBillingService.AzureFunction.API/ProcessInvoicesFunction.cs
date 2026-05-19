namespace CentralBillingService.AzureFunction.API;

internal class ProcessInvoicesFunction
{
    private readonly ILogger<ProcessInvoicesFunction> Logger;
    private readonly GenerateInvoiceUseCase GenerateInvoice;

    public ProcessInvoicesFunction(ILogger<ProcessInvoicesFunction> logger, GenerateInvoiceUseCase generateInvoice)
    {
        Logger = logger;
        GenerateInvoice = generateInvoice;
    }

    [Function("invoice-processor")]
    public async Task Run([QueueTrigger("invoices", Connection = "InvoiceCreateQueueStorage")] string message)
    {
        Logger.LogInformation("C# Queue trigger function processed: {messageText}", message);
        GenerateInvoiceReportCommand data = JsonSerializer.Deserialize<GenerateInvoiceReportCommand>(message);
        await GenerateInvoice.GenerateInvoice(data);
    }
}