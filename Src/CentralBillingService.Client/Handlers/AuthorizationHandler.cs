namespace CentralBillingService.Client.Handlers;

internal class AuthorizationHandler(IOptions<CbsOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("x-cbs-key", options.Value.AppSecret);
        request.Headers.TryAddWithoutValidation("x-cbs-billing-source", options.Value.BillingSource);
        request.Headers.TryAddWithoutValidation("x-functions-key", options.Value.AppKey);
        return await base.SendAsync(request, cancellationToken);
    }
}