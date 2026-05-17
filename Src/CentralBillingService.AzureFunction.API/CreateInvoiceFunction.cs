namespace CentralBillingService.AzureFunction.API;

public sealed class CreateInvoiceFunction
{
    private readonly CreateInvoiceUseCase _useCase;
    private readonly ILogger<CreateInvoiceFunction> _logger;

    public CreateInvoiceFunction(
        CreateInvoiceUseCase useCase,
        ILogger<CreateInvoiceFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(CreateInvoiceFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        CreateInvoiceCommand command;

        try
        {
            var request = await HttpRequestBodyHelper.GetRequestedModel<CreateInvoiceRequest>(req, cancellationToken);

            command = new CreateInvoiceCommand
            {
                BillingSource = RequestHelper.GetBillingSource(req),
                Secret = RequestHelper.GetSecret(req),
                Serie = request.Serie,
                Recipient = request.Recipient,
                Lines = request.Lines,
                OriginCurrencyCode = request.OriginCurrencyCode,
                IssueDate = request.IssueDate,
                ValueDate = request.ValueDate,
                Notes = request.Notes,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                TransactionData = request.TransactionData,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid request body for CreateInvoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest,
                "Invalid request body.",
                ex.Message);
        }

        try
        {
            var result = await _useCase.ExecuteAsync(command, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating invoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Validation error.", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating invoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.UnprocessableEntity, "Business rule violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating invoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
