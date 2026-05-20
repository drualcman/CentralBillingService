namespace CentralBillingService.VerifyUI.Models;

public sealed class InvoicesPeriodSummaryResponse
{
    public int Year { get; set; }
    public int? Period { get; set; }
    public string Label { get; set; } = "";
    public decimal TotalEur { get; set; }
    public decimal TaxableBaseEur { get; set; }
    public decimal TotalTaxAmountEur { get; set; }
    public int InvoiceCount { get; set; }
}

public sealed class InvoicesSummaryResponse
{
    public string? BillingSource { get; set; }
    public List<InvoicesPeriodSummaryResponse> Annual { get; set; } = [];
    public List<InvoicesPeriodSummaryResponse> Quarterly { get; set; } = [];
    public List<InvoicesPeriodSummaryResponse> FourMonthly { get; set; } = [];
}
