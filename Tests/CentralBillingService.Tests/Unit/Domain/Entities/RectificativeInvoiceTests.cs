namespace CentralBillingService.Tests.Unit.Domain.Entities;

public class RectificativeInvoiceTests
{
    private static readonly FakeInvoiceHasher Hasher = new();

    private static RectificativeInvoice CreateRectificative(
        Invoice? original = null,
        string reason = "Error en los datos del cliente",
        RectificationType type = RectificationType.Substitution,
        List<InvoiceLine>? lines = null)
    {
        original ??= InvoiceBuilder.BuildIssued();
        lines ??= [InvoiceBuilder.DefaultLine()];

        return RectificativeInvoice.Create(
            InvoiceNumber.Create("REC", 2026, 1),
            "web-test",
            original,
            reason,
            type,
            lines,
            ExchangeRate.Identity(DateTimeOffset.UtcNow),
            Hasher,
            "PAY-002");
    }

    [Fact]
    public void Create_references_original_invoice_data()
    {
        var original = InvoiceBuilder.BuildIssued();
        var rectificative = CreateRectificative(original);

        Assert.Equal(original.Number, rectificative.OriginalInvoiceNumber);
        Assert.Equal(original.IssueDate, rectificative.OriginalIssueDate);
        Assert.Equal(original.Issuer, rectificative.Issuer);
        Assert.Equal(original.Recipient, rectificative.Recipient);
    }

    [Fact]
    public void Create_starts_in_draft_status()
    {
        var rectificative = CreateRectificative();
        Assert.Equal(InvoiceStatus.Draft, rectificative.Status);
    }

    [Fact]
    public void Create_generates_non_empty_hash()
    {
        var rectificative = CreateRectificative();
        Assert.NotEmpty(rectificative.Hash);
    }

    [Fact]
    public void Create_stores_rectification_reason_and_type()
    {
        const string reason = "Dirección del cliente incorrecta";
        var rectificative = CreateRectificative(reason: reason);

        Assert.Equal(reason.Trim(), rectificative.RectificationReason);
        Assert.Equal(RectificationType.Substitution, rectificative.RectificationType);
    }

    [Fact]
    public void Create_on_draft_original_throws()
    {
        var draft = Invoice.Create(
            InvoiceNumber.Create("TEST", 2026, 1),
            "web-test",
            InvoiceBuilder.DefaultIssuer(),
            InvoiceBuilder.DefaultRecipient(),
            new DateOnly(2026, 5, 1),
            [InvoiceBuilder.DefaultLine()],
            ExchangeRate.Identity(DateTimeOffset.UtcNow),
            Hasher,
            "PAY-001");

        Assert.Throws<DomainException>(() => CreateRectificative(draft));
    }

    [Fact]
    public void Create_on_already_rectified_original_is_allowed()
    {
        var original = InvoiceBuilder.BuildIssued();
        original.MarkAsRectifiedBy(InvoiceNumber.Create("REC", 2026, 99));

        // Should not throw — an already-rectified invoice can be rectified again
        var rectificative = CreateRectificative(original);
        Assert.NotNull(rectificative);
    }

    [Fact]
    public void Create_empty_lines_throws()
    {
        var original = InvoiceBuilder.BuildIssued();
        Assert.Throws<DomainException>(() => RectificativeInvoice.Create(
            InvoiceNumber.Create("REC", 2026, 1),
            "web-test",
            original,
            "Motivo",
            RectificationType.Substitution,
            [],
            ExchangeRate.Identity(DateTimeOffset.UtcNow),
            Hasher,
            "PAY-002"));
    }

    [Fact]
    public void Create_computes_totals_from_lines()
    {
        var original = InvoiceBuilder.BuildIssued();
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateInEur(1, "Servicio", 1, Money.Of(100m, Currency.EUR), TaxRate.General)
        };

        var rectificative = CreateRectificative(original, lines: lines);

        Assert.Equal(Money.Of(100m, Currency.EUR), rectificative.TaxableBaseEur);
        Assert.Equal(Money.Of(21m, Currency.EUR), rectificative.TotalTaxAmountEur);
        Assert.Equal(Money.Of(121m, Currency.EUR), rectificative.TotalEur);
    }

    [Fact]
    public void Issue_transitions_to_issued_status()
    {
        var rectificative = CreateRectificative();
        rectificative.Issue();
        Assert.Equal(InvoiceStatus.Issued, rectificative.Status);
    }

    [Fact]
    public void Issue_twice_throws()
    {
        var rectificative = CreateRectificative();
        rectificative.Issue();
        Assert.Throws<DomainException>(() => rectificative.Issue());
    }

    [Fact]
    public void BuildHashContent_has_invoice_type_R()
    {
        var rectificative = CreateRectificative();
        var content = rectificative.BuildHashContent();
        Assert.Equal("R", content.InvoiceType);
    }
}
