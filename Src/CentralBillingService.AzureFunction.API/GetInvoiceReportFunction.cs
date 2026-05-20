namespace CentralBillingService.AzureFunction.API;

public sealed class GetInvoiceReportFunction
{
    private readonly ILogger<GetInvoiceReportFunction> _logger;
    private readonly GenerateInvoiceReportUseCase _generateInvoiceReport;
    private readonly IBlobStorageService _storageService;


    public GetInvoiceReportFunction(
        ILogger<GetInvoiceReportFunction> logger,
        GenerateInvoiceReportUseCase generateInvoiceReport,
        IBlobStorageService storageService)
    {
        _logger = logger;
        _generateInvoiceReport = generateInvoiceReport;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/invoices/{invoiceNumber}/report?billingsource={billingSource}
    /// Returns a ReportViewModel (JSON) for the invoice, ready to be rendered as HTML or PDF
    /// by the consumer. If the invoice has been tampered with, HasTamper is reflected visually
    /// in the report via a red banner.
    /// </summary>
    [Function(nameof(Report))]
    public async Task<HttpResponseData> Report(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{invoiceNumber}/report")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var billingSource = qs["billingsource"] ?? string.Empty;
            GenerateInvoiceReportCommand command = new(invoiceNumber, billingSource);
            var reportModel = await _generateInvoiceReport.GenerateInvoiceViewModel(command, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(reportModel, cancellationToken);
            return response;
        }
        catch (Exception ex) when (
            ex is InvoiceNotFoundException ||
            ex is NotFoundException)
        {
            _logger.LogWarning(ex, ex.Message);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
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

    /// <summary>
    /// GET /api/invoices/{invoiceNumber}/pdf?billingsource={billingSource}
    /// Returns a url for the invoice, ready in PDF
    /// </summary>
    [Function(nameof(PdfUrl))]
    public async Task<HttpResponseData> PdfUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{invoiceNumber}/pdf")]
        HttpRequestData req,
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var billingSource = qs["billingsource"] ?? string.Empty;
            string url = _storageService.GetInvoiceUrl(InvoiceHelper.GetInvoiceFileName(billingSource, invoiceNumber));
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(url, cancellationToken);
            return response;
        }
        catch (Exception ex) when (
            ex is InvoiceNotFoundException ||
            ex is NotFoundException)
        {
            _logger.LogWarning(ex, ex.Message);
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.NotFound, "Invoice not found.", ex.Message);
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
