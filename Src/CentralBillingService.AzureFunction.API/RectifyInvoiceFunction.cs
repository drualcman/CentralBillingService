using CentralBillingService.AzureFunction.API.Requests;

namespace CentralBillingService.AzureFunction.API;

public sealed class RectifyInvoiceFunction
{
    private readonly RectifyInvoiceUseCase _useCase;
    private readonly ILogger<RectifyInvoiceFunction> _logger;

    public RectifyInvoiceFunction(
        RectifyInvoiceUseCase useCase,
        ILogger<RectifyInvoiceFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(RectifyInvoiceFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices/{invoiceNumber}/rectify")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        RectifyInvoiceCommand command;

        try
        {
            var request = await HttpRequestBodyHelper.GetRequestedModel<RectifyInvoiceRequest>(req, cancellationToken);

            // The invoice number comes from the route — override whatever the body says
            command = new RectifyInvoiceCommand()
            {
                BillingSource = RequestHelper.GetBillingSource(req),
                Secret = RequestHelper.GetSecret(req),
                OriginalInvoiceNumber = invoiceNumber,
                RectificationType = request.RectificationType,
                RectificativeSerie = request.RectificativeSerie,
                Reason = request.Reason,
                IssueDate = request.IssueDate,
                Lines = request.Lines,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                TransactionData = request.TransactionData,
                Notes = request.Notes
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid request body for RectifyInvoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Invalid request body.", ex.Message);
        }

        try
        {
            var result = await _useCase.ExecuteAsync(command, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (InvoiceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invoice {InvoiceNumber} not found for rectification.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
        }
        catch (InvoiceTamperingDetectedException ex)
        {
            _logger.LogCritical(ex,
                "DATA INTEGRITY VIOLATION: Invoice {InvoiceNumber} hash mismatch during rectification. Possible tampering.",
                invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Data integrity violation.", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error rectifying invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Validation error.", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation rectifying invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.UnprocessableEntity, "Business rule violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error rectifying invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
