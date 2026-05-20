namespace CentralBillingService.AzureFunction.API;

/// <summary>
/// Public endpoint — no API key required.
/// Returns billing totals grouped by annual, quarterly (Q1–Q4), and four-monthly (T1–T3) periods.
///
/// GET /api/public/invoices/summary
/// Query: billingSource? (omit to aggregate all sources)
/// </summary>
public sealed class GetInvoicesSummaryFunction
{
    private readonly GetInvoicesSummaryUseCase _useCase;
    private readonly ILogger<GetInvoicesSummaryFunction> _logger;

    public GetInvoicesSummaryFunction(
        GetInvoicesSummaryUseCase useCase,
        ILogger<GetInvoicesSummaryFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(GetInvoicesSummaryFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/invoices/summary")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var billingSource = qs["billingSource"];

            var result = await _useCase.ExecuteAsync(billingSource, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating invoices summary.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
