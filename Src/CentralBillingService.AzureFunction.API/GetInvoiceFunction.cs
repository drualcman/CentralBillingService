namespace CentralBillingService.AzureFunction.API;

public sealed class GetInvoiceFunction
{
    private readonly GetInvoiceUseCase _useCase;
    private readonly ILogger<GetInvoiceFunction> _logger;

    public GetInvoiceFunction(
        GetInvoiceUseCase useCase,
        ILogger<GetInvoiceFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/invoices/{invoiceNumber}
    /// invoiceNumber can be the formatted number (FOTO2026-0001) or a UUID.
    /// </summary>
    [Function(nameof(GetInvoiceFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices/{invoiceNumber}")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            // Accept both UUID and formatted number
            var query = Guid.TryParse(invoiceNumber, out var id)
                ? new GetInvoiceQuery { BillingSource = RequestHelper.GetBillingSource(req), Secret = RequestHelper.GetSecret(req), Id = id }
                : new GetInvoiceQuery { BillingSource = RequestHelper.GetBillingSource(req), Secret = RequestHelper.GetSecret(req), InvoiceNumber = invoiceNumber };

            var result = await _useCase.ExecuteAsync(query, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (InvoiceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invoice {InvoiceNumber} not found.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
        }
        catch (InvoiceTamperingDetectedException ex)
        {
            _logger.LogCritical(ex,
                "DATA INTEGRITY VIOLATION: Invoice {InvoiceNumber} hash mismatch. Possible tampering.",
                invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Data integrity violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
