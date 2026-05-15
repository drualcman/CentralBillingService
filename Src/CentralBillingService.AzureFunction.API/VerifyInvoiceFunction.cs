namespace CentralBillingService.AzureFunction.API;

public sealed class VerifyInvoiceFunction
{
    private readonly VerifyInvoiceIntegrityUseCase _useCase;
    private readonly ILogger<VerifyInvoiceFunction> _logger;

    public VerifyInvoiceFunction(
        VerifyInvoiceIntegrityUseCase useCase,
        ILogger<VerifyInvoiceFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/invoices/{invoiceNumber}/verify?hash={documentHash}
    ///
    /// Verifies two things:
    ///   1. The hash from the customer's QR matches the stored hash (document authenticity).
    ///   2. The stored hash matches a fresh recomputation from DB fields (tamper detection).
    ///
    /// Required by Real Decreto 1007/2023 (VeriFactu). When an external registry is
    /// available, an additional check against it will be performed here.
    /// </summary>
    [Function(nameof(VerifyInvoiceFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices/{invoiceNumber}/verify")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var providedHash = qs["hash"];

        if (string.IsNullOrWhiteSpace(providedHash))
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Hash required.", "A document hash must be provided for verification.");

        try
        {
            var query = new VerifyInvoiceQuery
            {
                BillingSource = RequestHelper.GetBillingSource(req),
                Secret = RequestHelper.GetSecret(req),
                InvoiceNumber = invoiceNumber,
                ProvidedHash = providedHash,
            };

            var result = await _useCase.ExecuteAsync(query, cancellationToken);

            if (!result.DocumentHashMatches)
                _logger.LogWarning(
                    "Document hash mismatch for invoice {InvoiceNumber}. ProvidedHash: {ProvidedHash}, StoredHash: {StoredHash}",
                    invoiceNumber, providedHash, result.Hash);

            if (!result.IntegrityVerified)
                _logger.LogWarning(
                    "Integrity check failed for invoice {InvoiceNumber}. StoredHash: {Hash}",
                    invoiceNumber, result.Hash);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (InvoiceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invoice {InvoiceNumber} not found for verification.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
