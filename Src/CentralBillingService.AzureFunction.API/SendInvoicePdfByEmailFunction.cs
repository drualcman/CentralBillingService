namespace CentralBillingService.AzureFunction.API;

public sealed class SendInvoicePdfByEmailFunction
{
    private readonly SendInvoicePdfByEmailUseCase _useCase;
    private readonly ILogger<SendInvoicePdfByEmailFunction> _logger;

    public SendInvoicePdfByEmailFunction(
        SendInvoicePdfByEmailUseCase useCase,
        ILogger<SendInvoicePdfByEmailFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/invoices/{invoiceNumber}/send-pdf
    /// Sends the stored invoice PDF to the recipient's registered email address.
    /// Returns 200 with a result indicating success or no-email-configured.
    /// </summary>
    [Function(nameof(SendInvoicePdfByEmailFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices/{invoiceNumber}/send-pdf")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new SendInvoicePdfByEmailQuery
            {
                BillingSource = RequestHelper.GetBillingSource(req),
                InvoiceNumber = invoiceNumber
            };

            var result = await _useCase.ExecuteAsync(query, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is InvoiceNotFoundException || ex is NotFoundException)
        {
            _logger.LogWarning(ex, "Invoice {InvoiceNumber} not found.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation sending invoice PDF.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.UnprocessableEntity, "Business rule violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending invoice PDF for {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
