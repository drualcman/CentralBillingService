namespace CentralBillingService.Tests.Unit.Domain.Entities;

public class InvoiceTests
{
    private static readonly FakeInvoiceHasher Hasher = new();
    private static readonly BillingParty Issuer = InvoiceBuilder.DefaultIssuer();
    private static readonly BillingParty Recipient = InvoiceBuilder.DefaultRecipient();
    private static readonly ExchangeRate IdentityRate = ExchangeRate.Identity(DateTimeOffset.UtcNow);

    private static Invoice CreateDraft(List<InvoiceLine>? lines = null) =>
        Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test",
            Issuer,
            Recipient,
            new DateOnly(2026, 5, 1),
            lines ?? [InvoiceBuilder.DefaultLine()],
            IdentityRate,
            Hasher,
            "PAY-001");

    [Fact]
    public void Create_starts_in_draft_status()
    {
        var invoice = CreateDraft();
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void Create_generates_non_empty_hash()
    {
        var invoice = CreateDraft();
        Assert.NotEmpty(invoice.Hash);
    }

    [Fact]
    public void Create_sets_previous_hash_when_provided()
    {
        const string prevHash = "PREVIOUS_HASH";
        var invoice = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test", Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            IdentityRate, Hasher,
            "PAY-001",
            previousHash: prevHash);

        Assert.Equal(prevHash, invoice.PreviousHash);
    }

    [Fact]
    public void Create_previous_hash_null_by_default()
    {
        var invoice = CreateDraft();
        Assert.Null(invoice.PreviousHash);
    }

    [Fact]
    public void Create_normalizes_billing_source_to_lowercase()
    {
        var invoice = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "Web-Fotos",
            Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            IdentityRate, Hasher,
            "PAY-001");

        Assert.Equal("web-fotos", invoice.BillingSource);
    }

    [Fact]
    public void Create_computes_totals_from_multiple_lines()
    {
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateInEur(1, "Servicio A", 1, Money.Of(100m, Currency.EUR), TaxRate.General),
            InvoiceLine.CreateInEur(2, "Servicio B", 2, Money.Of(50m, Currency.EUR), TaxRate.Reduced),
        };

        var invoice = CreateDraft(lines);

        // Line 1: taxable=100, tax=21
        // Line 2: taxable=100, tax=10
        Assert.Equal(Money.Of(200m, Currency.EUR), invoice.TaxableBaseEur);
        Assert.Equal(Money.Of(31m, Currency.EUR), invoice.TotalTaxAmountEur);
        Assert.Equal(Money.Of(231m, Currency.EUR), invoice.TotalEur);
    }

    [Fact]
    public void Create_total_in_origin_currency_is_pre_tax_price_for_eur_invoice()
    {
        // For EUR invoices, TotalInOriginCurrency = sum of (unitPrice × qty), no VAT
        var invoice = CreateDraft();  // 1 line: 100 EUR × 1 qty

        Assert.Equal(Currency.EUR, invoice.TotalInOriginCurrency.Currency);
        Assert.Equal(100m, invoice.TotalInOriginCurrency.Amount); // pre-tax, not 121
    }

    [Fact]
    public void Create_total_in_origin_currency_in_foreign_currency()
    {
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateWithConversion(
                1, "Service", 1,
                Money.Of(100m, Currency.USD),
                Money.Of(92m, Currency.EUR),
                TaxRate.General)
        };

        var invoice = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test", Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            lines, rate, Hasher,
            "PAY-001");

        Assert.Equal(Money.Of(100m, Currency.USD), invoice.TotalInOriginCurrency);
    }

    [Fact]
    public void IsInOriginCurrency_false_for_eur_invoice()
    {
        var invoice = CreateDraft();
        Assert.False(invoice.IsInOriginCurrency);
    }

    [Fact]
    public void IsInOriginCurrency_true_for_foreign_currency_invoice()
    {
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateWithConversion(
                1, "Service", 1,
                Money.Of(100m, Currency.USD),
                Money.Of(92m, Currency.EUR),
                TaxRate.Zero)
        };

        var invoice = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test", Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            lines, rate, Hasher,
            "PAY-001");

        Assert.True(invoice.IsInOriginCurrency);
    }

    [Fact]
    public void Issue_transitions_to_issued_status()
    {
        var invoice = CreateDraft();
        invoice.Issue();
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void Issue_twice_throws()
    {
        var invoice = CreateDraft();
        invoice.Issue();
        Assert.Throws<DomainException>(() => invoice.Issue());
    }

    [Fact]
    public void MarkAsRectifiedBy_transitions_to_rectified_and_stores_number()
    {
        var invoice = CreateDraft();
        invoice.Issue();
        var rectNumber = InvoiceNumber.Create("REC", 2026, 1);

        invoice.MarkAsRectifiedBy(rectNumber);

        Assert.Equal(InvoiceStatus.Rectified, invoice.Status);
        Assert.Equal(rectNumber, invoice.RectifiedBy);
    }

    [Fact]
    public void MarkAsRectifiedBy_on_draft_throws()
    {
        var invoice = CreateDraft();
        var rectNumber = InvoiceNumber.Create("REC", 2026, 1);
        Assert.Throws<DomainException>(() => invoice.MarkAsRectifiedBy(rectNumber));
    }

    [Fact]
    public void Create_empty_billing_source_throws()
    {
        Assert.Throws<DomainException>(() => Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "",
            Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            IdentityRate, Hasher,
            "PAY-001"));
    }

    [Fact]
    public void Create_empty_payment_reference_throws()
    {
        Assert.Throws<DomainException>(() => Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test",
            Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            IdentityRate, Hasher,
            ""));
    }

    [Fact]
    public void Create_no_lines_throws()
    {
        Assert.Throws<DomainException>(() => Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test",
            Issuer, Recipient,
            new DateOnly(2026, 5, 1),
            [],
            IdentityRate, Hasher,
            "PAY-001"));
    }
}
