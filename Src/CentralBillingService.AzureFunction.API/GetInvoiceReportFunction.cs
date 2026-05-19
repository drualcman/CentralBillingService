namespace CentralBillingService.AzureFunction.API;

public sealed class GetInvoiceReportFunction
{
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceHasher _hasher;
    private readonly ILogger<GetInvoiceReportFunction> _logger;

    public GetInvoiceReportFunction(
        IInvoiceRepository repository,
        IInvoiceHasher hasher,
        ILogger<GetInvoiceReportFunction> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
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
            var invoice = Guid.TryParse(invoiceNumber, out var id)
                ? await _repository.FindByIdAsync(billingSource, id, cancellationToken)
                : await _repository.FindByNumberAsync(billingSource, invoiceNumber, cancellationToken);

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

            var reportModel = await GenerateInvoiceReport.BuildAsync(invoice);

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
