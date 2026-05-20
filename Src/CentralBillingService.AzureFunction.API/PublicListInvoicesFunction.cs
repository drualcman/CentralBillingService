namespace CentralBillingService.AzureFunction.API;

/// <summary>
/// Public endpoint — no API key required.
/// Returns a paginated list of invoices across all (or one) billing sources.
/// Intended for the VerifyUI portal.
///
/// GET /api/public/invoices
/// Query: billingSource?, page?, pageSize?, issuedFrom?, issuedTo?, status?
/// </summary>
public sealed class PublicListInvoicesFunction
{
    private readonly PublicListInvoicesUseCase _useCase;
    private readonly ILogger<PublicListInvoicesFunction> _logger;

    public PublicListInvoicesFunction(
        PublicListInvoicesUseCase useCase,
        ILogger<PublicListInvoicesFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(PublicListInvoicesFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/invoices")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            DateOnly? ParseDate(string? v) =>
                DateOnly.TryParse(v, out var d) ? d : null;

            int ParseInt(string? v, int fallback) =>
                int.TryParse(v, out var n) ? n : fallback;

            var query = new ListInvoicesQuery
            {
                BillingSource = qs["billingSource"],
                IssuedFrom = ParseDate(qs["issuedFrom"]),
                IssuedTo = ParseDate(qs["issuedTo"]),
                Status = qs["status"],
                Page = ParseInt(qs["page"], 1),
                PageSize = ParseInt(qs["pageSize"], 25),
            };

            var result = await _useCase.ExecuteAsync(query, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query parameters for PublicListInvoices.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Invalid query parameters.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing invoices (public).");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
