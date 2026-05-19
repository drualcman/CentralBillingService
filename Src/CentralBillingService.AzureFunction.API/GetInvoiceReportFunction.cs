using CentralBillingService.Domain.Entities;
using CentralBillingService.Domain.Services;

namespace CentralBillingService.AzureFunction.API;

public sealed class GetInvoiceReportFunction
{
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceHasher _hasher;
    private readonly ILogger<GetInvoiceReportFunction> _logger;
    private readonly BillingSourceRegistry _registry;
    private readonly GetInvoiceUseCase _invoiceUseCase;


    public GetInvoiceReportFunction(
        IInvoiceRepository repository,
        IInvoiceHasher hasher,
        ILogger<GetInvoiceReportFunction> logger,
        BillingSourceRegistry registry,
        GetInvoiceUseCase invoiceUseCase)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
        _registry = registry;
        _invoiceUseCase = invoiceUseCase;
    }

    /// <summary>
    /// GET /api/invoices/{invoiceNumber}/report?billingsource={billingSource}
    /// Returns a ReportViewModel (JSON) for the invoice, ready to be rendered as HTML or PDF
    /// by the consumer. If the invoice has been tampered with, HasTamper is reflected visually
    /// in the report via a red banner.
    /// </summary>
    [Function(nameof(GetInvoiceReportFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{invoiceNumber}/report")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var billingSource = qs["billingsource"] ?? string.Empty;

        try
        {
            // 2. Validate that the billing source exists before doing anything else
            var config = _registry.GetConfig(billingSource);

            var invoiceQuery = await _invoiceUseCase.ExecuteAsync(new GetInvoiceQuery
            {
                BillingSource = billingSource,
                InvoiceNumber = invoiceNumber,
                Secret = config.Secret
            });

            Invoice? invoice;
            if (invoiceQuery.IsRectificative)
            {
                var rectificative = await _repository.FindRectificativeByNumberAsync(billingSource, invoiceQuery.InvoiceNumber, cancellationToken);
                if (rectificative is not null)
                    invoice = Invoice.Reconstitute(rectificative.Id, rectificative.Number, rectificative.BillingSource, rectificative.Issuer,
                        rectificative.Recipient, rectificative.IssueDate, null, rectificative.CreatedAt, rectificative.Lines.ToList(),
                        rectificative.AppliedExchangeRate, rectificative.Hash, rectificative.PreviousHash, rectificative.Status,
                        rectificative.PaymentReference, null, rectificative.Notes, rectificative.TransactionData, rectificative.PaymentMethod,
                        rectificative.QrCodeBlobUrl);
                else
                    invoice = null;
            }
            else
                invoice = await _repository.FindByIdAsync(billingSource, invoiceQuery.Id, cancellationToken);

            if (invoice is null)
            {
                _logger.LogWarning("Invoice {InvoiceNumber} not found for report generation.", invoiceNumber);
                return await req.CreateProblemResponseAsync(
                    HttpStatusCode.NotFound, "Invoice not found.", $"No invoice found for '{invoiceNumber}'.");
            }

            invoice.VerifyIntegrity(_hasher);

            if (invoice.HasTamper)
                _logger.LogWarning(
                    "DATA INTEGRITY WARNING: Invoice {InvoiceNumber} has been tampered with. Report will show warning banner.",
                    invoiceNumber);

            var logoUrl = string.IsNullOrWhiteSpace(config.Issuer.LogoUrl) ? "https://drualcman.blob.core.windows.net/content/SergiLogo.png" : config.Issuer.LogoUrl;

            var reportModel = await GenerateInvoiceReport.BuildAsync(invoice, logoUrl);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(reportModel, cancellationToken);
            return response;
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating invoice.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.UnprocessableEntity, "Business rule violation.", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for invoice {InvoiceNumber}.", invoiceNumber);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Report generation failed.", "An internal error occurred.");
        }
    }
}
