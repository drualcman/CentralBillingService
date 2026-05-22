namespace CentralBillingService.AzureFunction.API;

public record PublicBillingSourceItem(string Code, string DisplayName);

/// <summary>
/// Public endpoint — no API key required.
/// Returns the list of registered billing source identifiers.
/// Used by the VerifyUI to populate filter dropdowns.
///
/// GET /api/public/billing-sources
/// </summary>
public sealed class GetBillingSourcesFunction
{
    private readonly BillingSourceRegistry _registry;

    public GetBillingSourcesFunction(BillingSourceRegistry registry)
    {
        _registry = registry;
    }

    [Function(nameof(GetBillingSourcesFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/billing-sources")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var sources = _registry.GetAll()
            .Select(c => new PublicBillingSourceItem(
                c.BillingSource,
                !string.IsNullOrWhiteSpace(c.Issuer.TradeName) ? c.Issuer.TradeName! : c.Issuer.LegalName))
            .OrderBy(s => s.DisplayName)
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(sources, cancellationToken);
        return response;
    }
}
