namespace CentralBillingService.Tests.Helpers;

public static class InvoiceBuilder
{
    private static readonly FakeInvoiceHasher Hasher = new();

    public static BillingParty DefaultIssuer() => BillingParty.Create(
        legalName: "Test Autónomo SA",
        taxId: TaxId.Create("12345678A", "ES"),
        address: PostalAddress.Create("Calle Test 1", "Barcelona", "08001", "ES"),
        email: "issuer@test.com",
        tradeName: "Test Issuer");

    public static BillingParty DefaultRecipient() => BillingParty.Create(
        legalName: "ACME Corp SL",
        taxId: TaxId.Create("B12345678", "ES"),
        address: PostalAddress.Create("Calle Client 1", "Madrid", "28001", "ES"),
        email: "client@acme.com");

    public static InvoiceLine DefaultLine(
        int lineNumber = 1,
        decimal unitPrice = 100m,
        int quantity = 1,
        TaxRate? taxRate = null) =>
        InvoiceLine.CreateInEur(
            lineNumber,
            "Test service",
            quantity,
            Money.Of(unitPrice, Currency.EUR),
            taxRate ?? TaxRate.General);

    public static BillingSourceConfig DefaultBillingSourceConfig(
        string billingSource = "web-test",
        string secret = "secret123") =>
        new()
        {
            BillingSource = billingSource,
            Secret = secret,
            Issuer = IssuerConfig.From(DefaultIssuer())
        };

    public static BillingSourceRegistry DefaultRegistry(
        string billingSource = "web-test",
        string secret = "secret123")
    {
        var options = Options.Create(new CbsOptions
        {
            BillingSources = [DefaultBillingSourceConfig(billingSource, secret)]
        });
        return new BillingSourceRegistry(options);
    }

    /// <summary>Creates an issued invoice ready for use in tests.</summary>
    public static Invoice BuildIssued(
        string serie = "TEST",
        int number = 1,
        string billingSource = "web-test",
        BillingParty? issuer = null,
        BillingParty? recipient = null,
        List<InvoiceLine>? lines = null,
        string paymentReference = "PAY-001",
        ExchangeRate? exchangeRate = null,
        IInvoiceHasher? hasher = null,
        string? previousHash = null,
        DateOnly? issueDate = null)
    {
        var invoiceNumber = InvoiceNumber.Create(serie, 2026, number);
        var rate = exchangeRate ?? ExchangeRate.Identity(DateTimeOffset.UtcNow);

        var invoice = Invoice.Create(
            number: invoiceNumber,
            billingSource: billingSource,
            issuer: issuer ?? DefaultIssuer(),
            recipient: recipient ?? DefaultRecipient(),
            issueDate: issueDate ?? new DateOnly(2026, 1, 15),
            lines: lines ?? [DefaultLine()],
            appliedExchangeRate: rate,
            hasher: hasher ?? Hasher,
            paymentReference: paymentReference,
            previousHash: previousHash);

        invoice.Issue();
        return invoice;
    }

    public static RectificativeInvoice BuildIssuedRectificative(
        string serie = "REC",
        int number = 1,
        string billingSource = "web-test",
        IInvoiceHasher? hasher = null,
        string? previousHash = null)
    {
        var originalInvoice = BuildIssued(billingSource: billingSource, hasher: hasher ?? Hasher);
        var rectNumber = InvoiceNumber.Create(serie, 2026, number);
        var rectificative = RectificativeInvoice.Create(
            number: rectNumber,
            billingSource: billingSource,
            originalInvoice: originalInvoice,
            rectificationReason: "Test rectification",
            rectificationType: RectificationType.Substitution,
            lines: [DefaultLine()],
            appliedExchangeRate: ExchangeRate.Identity(DateTimeOffset.UtcNow),
            hasher: hasher ?? Hasher,
            issueDate: new DateOnly(2026, 1, 15),
            paymentReference: "PAY-REC-001",
            previousHash: previousHash);
        rectificative.Issue();
        return rectificative;
    }
}
