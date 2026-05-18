namespace CentralBillingService.AzureFunction.API;

public sealed class ListInvoicesFunction
{
    private readonly ListInvoicesUseCase _useCase;
    private readonly ILogger<ListInvoicesFunction> _logger;

    public ListInvoicesFunction(
        ListInvoicesUseCase useCase,
        ILogger<ListInvoicesFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/invoices
    /// Query string parameters (all optional):
    ///   billingSource, serie, year, issuedFrom (yyyy-MM-dd), issuedTo (yyyy-MM-dd),
    ///   recipientTaxId, status, page (default 1), pageSize (default 25, max 100)
    /// </summary>
    [Function(nameof(ListInvoicesFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = ParseQuery(req);
            var result = await _useCase.ExecuteAsync(query, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query parameters for ListInvoices.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Invalid query parameters.", ex.Message);
        }
        catch (InvoiceTamperingDetectedException ex)
        {
            _logger.LogCritical(ex,
                "DATA INTEGRITY VIOLATION: Invoice {InvoiceNumber} hash mismatch detected during list. Possible tampering.",
                ex.InvoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Data integrity violation.", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating invoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.UnprocessableEntity, "Business rule violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing invoices.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }

    private static ListInvoicesQuery ParseQuery(HttpRequestData req)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        DateOnly? ParseDate(string? value) =>
            DateOnly.TryParse(value, out var d) ? d : null;

        int ParseInt(string? value, int fallback) =>
            int.TryParse(value, out var n) ? n : fallback;

        return new ListInvoicesQuery
        {
            BillingSource = RequestHelper.GetBillingSource(req),
            Secret = RequestHelper.GetSecret(req),
            Serie = qs["serie"],
            Year = int.TryParse(qs["year"], out var y) ? y : null,
            IssuedFrom = ParseDate(qs["issuedFrom"]),
            IssuedTo = ParseDate(qs["issuedTo"]),
            RecipientTaxId = qs["recipientTaxId"],
            RecipientExternalId = qs["recipientExternalId"],
            Status = qs["status"],
            Page = ParseInt(qs["page"], 1),
            PageSize = ParseInt(qs["pageSize"], 25),
        };
    }
}
