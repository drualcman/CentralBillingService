namespace CentralBillingService.Tests.Unit.Domain.Services;

public class RectifyInvoiceServiceTests
{
    private readonly RectifyInvoiceService _service;

    public RectifyInvoiceServiceTests()
    {
        var registry = InvoiceBuilder.DefaultRegistry("web-fotos", "secret123");
        _service = new RectifyInvoiceService(registry, new FakeExchangeRateProvider(), new FakeInvoiceHasher());
    }

    private static RectifyInvoiceRequest BuildSubstitutionRequest() => new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        Reason = "Error en los datos del cliente",
        RectificativeSerie = "REC",
        RectificationType = RectificationType.Substitution,
        PaymentReference = "PAY-002"
    };

    [Fact]
    public async Task ExecuteAsync_substitution_issues_rectificative()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var result = await _service.ExecuteAsync(BuildSubstitutionRequest(), original, reservedNumber: 1, previousHash: null);

        Assert.Equal(InvoiceStatus.Issued, result.Rectificative.Status);
        Assert.Equal(original.Number, result.Rectificative.OriginalInvoiceNumber);
        Assert.Equal(RectificationType.Substitution, result.Rectificative.RectificationType);
    }

    [Fact]
    public async Task ExecuteAsync_substitution_marks_original_as_rectified()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var result = await _service.ExecuteAsync(BuildSubstitutionRequest(), original, 1, null);

        Assert.Equal(InvoiceStatus.Rectified, result.UpdatedOriginal.Status);
        Assert.Equal(result.Rectificative.Number, result.UpdatedOriginal.RectifiedBy);
    }

    [Fact]
    public async Task ExecuteAsync_substitution_copies_original_lines()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var result = await _service.ExecuteAsync(BuildSubstitutionRequest(), original, 1, null);

        Assert.Equal(original.Lines.Count, result.Rectificative.Lines.Count);
        Assert.Equal(original.Lines[0].Description, result.Rectificative.Lines[0].Description);
        Assert.Equal(original.Lines[0].TaxableBaseEur, result.Rectificative.Lines[0].TaxableBaseEur);
    }

    [Fact]
    public async Task ExecuteAsync_substitution_uses_reserved_number_for_rectificative()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var result = await _service.ExecuteAsync(BuildSubstitutionRequest(), original, reservedNumber: 5, previousHash: null);

        Assert.Equal("REC2026-0005", result.Rectificative.Number.Value);
    }

    [Fact]
    public async Task ExecuteAsync_difference_uses_provided_lines()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var request = new RectifyInvoiceRequest
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            Reason = "Descuento no aplicado",
            RectificativeSerie = "REC",
            RectificationType = RectificationType.Difference,
            PaymentReference = "PAY-002",
            Lines =
            [
                new InvoiceLineData
                {
                    Description = "Descuento aplicado",
                    Quantity = 1,
                    UnitPrice = 10m,
                    TaxRatePercentage = 21
                }
            ]
        };

        var result = await _service.ExecuteAsync(request, original, 1, null);

        Assert.Single(result.Rectificative.Lines);
        Assert.Equal("Descuento aplicado", result.Rectificative.Lines[0].Description);
        Assert.Equal(RectificationType.Difference, result.Rectificative.RectificationType);
    }

    [Fact]
    public async Task ExecuteAsync_on_draft_original_throws()
    {
        var draft = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-fotos",
            InvoiceBuilder.DefaultIssuer(),
            InvoiceBuilder.DefaultRecipient(),
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            ExchangeRate.Identity(DateTimeOffset.UtcNow),
            new FakeInvoiceHasher(),
            "PAY-001");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ExecuteAsync(BuildSubstitutionRequest(), draft, 1, null));
    }

    [Fact]
    public async Task ExecuteAsync_unknown_billing_source_throws()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        var request = new RectifyInvoiceRequest
        {
            BillingSource = "unknown-source",
            Secret = "secret123",
            Reason = "Error en los datos",
            RectificativeSerie = "REC",
            RectificationType = RectificationType.Substitution,
            PaymentReference = "PAY-002"
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ExecuteAsync(request, original, 1, null));
    }

    [Fact]
    public async Task ExecuteAsync_sets_previous_hash_in_rectificative()
    {
        var original = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        const string prevHash = "HASH_ANTERIOR_REC";

        var result = await _service.ExecuteAsync(BuildSubstitutionRequest(), original, 1, prevHash);

        Assert.Equal(prevHash, result.Rectificative.PreviousHash);
    }
}
