namespace CentralBillingService.Tests.Unit.Domain.Services;

public class CreateInvoiceServiceTests
{
    private readonly CreateInvoiceService _service;

    public CreateInvoiceServiceTests()
    {
        var registry = InvoiceBuilder.DefaultRegistry("web-fotos", "secret123");
        _service = new CreateInvoiceService(registry, new FakeExchangeRateProvider(), new FakeInvoiceHasher());
    }

    private static CreateInvoiceRequest BuildRequest(string currencyCode = "EUR") => new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        Serie = "FOTO",
        OriginCurrencyCode = currencyCode,
        IssueDate = new DateOnly(2026, 5, 1),
        PaymentReference = "PAY-001",
        Recipient = new RecipientData
        {
            LegalName = "ACME Corp SL",
            TaxIdValue = "B12345678",
            TaxIdCountryCode = "ES",
            Email = "client@acme.com",
            AddressLine1 = "Calle Client 1",
            City = "Madrid",
            PostalCode = "28001",
            AddressCountryCode = "ES"
        },
        Lines =
        [
            new InvoiceLineData
            {
                Description = "Servicio fotográfico",
                Quantity = 1,
                UnitPrice = 100m,
                TaxRatePercentage = 21
            }
        ]
    };

    [Fact]
    public async Task ExecuteAsync_eur_invoice_is_issued_with_correct_totals()
    {
        var invoice = await _service.ExecuteAsync(BuildRequest("EUR"), reservedNumber: 1, previousHash: null);

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("web-fotos", invoice.BillingSource);
        Assert.Equal(Money.Of(100m, Currency.EUR), invoice.TaxableBaseEur);
        Assert.Equal(Money.Of(21m, Currency.EUR), invoice.TotalTaxAmountEur);
        Assert.Equal(Money.Of(121m, Currency.EUR), invoice.TotalEur);
    }

    [Fact]
    public async Task ExecuteAsync_eur_invoice_has_identity_exchange_rate()
    {
        var invoice = await _service.ExecuteAsync(BuildRequest("EUR"), 1, null);
        Assert.True(invoice.AppliedExchangeRate.IsIdentity);
    }

    [Fact]
    public async Task ExecuteAsync_uses_reserved_number_for_invoice_number()
    {
        var invoice = await _service.ExecuteAsync(BuildRequest("EUR"), reservedNumber: 42, previousHash: null);
        Assert.Equal("FOTO2026-0042", invoice.Number.Value);
    }

    [Fact]
    public async Task ExecuteAsync_sets_previous_hash_in_invoice()
    {
        const string prevHash = "HASH_ANTERIOR";
        var invoice = await _service.ExecuteAsync(BuildRequest("EUR"), 1, prevHash);
        Assert.Equal(prevHash, invoice.PreviousHash);
    }

    [Fact]
    public async Task ExecuteAsync_usd_invoice_converts_to_eur()
    {
        var invoice = await _service.ExecuteAsync(BuildRequest("USD"), 1, null);

        Assert.Equal(Currency.USD, invoice.AppliedExchangeRate.From);
        Assert.True(invoice.IsInOriginCurrency);
        // 100 USD × 0.92 = 92 EUR
        Assert.Equal(Money.Of(92m, Currency.EUR), invoice.TaxableBaseEur);
        Assert.Equal(Money.Of(100m, Currency.USD), invoice.TotalInOriginCurrency);
    }

    [Fact]
    public async Task ExecuteAsync_unknown_billing_source_throws()
    {
        var request = new CreateInvoiceRequest
        {
            BillingSource = "unknown-source",
            Secret = "secret123",
            Serie = "FOTO",
            OriginCurrencyCode = "EUR",
            PaymentReference = "PAY-001",
            Recipient = BuildRequest().Recipient,
            Lines = BuildRequest().Lines
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ExecuteAsync(request, 1, null));
    }

    [Fact]
    public async Task ExecuteAsync_wrong_secret_throws()
    {
        var request = new CreateInvoiceRequest
        {
            BillingSource = "web-fotos",
            Secret = "wrong-secret",
            Serie = "FOTO",
            OriginCurrencyCode = "EUR",
            PaymentReference = "PAY-001",
            Recipient = BuildRequest().Recipient,
            Lines = BuildRequest().Lines
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ExecuteAsync(request, 1, null));
    }

    [Fact]
    public async Task ExecuteAsync_unsupported_currency_throws()
    {
        // CHF is not in FakeExchangeRateProvider
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ExecuteAsync(BuildRequest("CHF"), 1, null));
    }

    [Fact]
    public async Task ExecuteAsync_multiple_lines_aggregates_totals()
    {
        var request = new CreateInvoiceRequest
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            Serie = "FOTO",
            OriginCurrencyCode = "EUR",
            PaymentReference = "PAY-001",
            Recipient = BuildRequest().Recipient,
            Lines =
            [
                new InvoiceLineData { Description = "Servicio A", Quantity = 1, UnitPrice = 100m, TaxRatePercentage = 21 },
                new InvoiceLineData { Description = "Servicio B", Quantity = 2, UnitPrice = 50m, TaxRatePercentage = 10 },
            ]
        };

        var invoice = await _service.ExecuteAsync(request, 1, null);

        // Line 1: base=100, tax=21  Line 2: base=100, tax=10
        Assert.Equal(Money.Of(200m, Currency.EUR), invoice.TaxableBaseEur);
        Assert.Equal(Money.Of(31m, Currency.EUR), invoice.TotalTaxAmountEur);
        Assert.Equal(Money.Of(231m, Currency.EUR), invoice.TotalEur);
    }
}
