namespace CentralBillingService.Tests.Unit.Domain.Entities;

public class InvoiceIntegrityTests
{
    private static readonly Sha256InvoiceHasher RealHasher = new();

    private static Invoice CreateIssuedInvoice(IInvoiceHasher? hasher = null) =>
        InvoiceBuilder.BuildIssued(
            serie: "TEST", number: 1,
            billingSource: "web-test",
            hasher: hasher ?? RealHasher);

    // ── VerifyIntegrity ────────────────────────────────────────────────────

    [Fact]
    public void VerifyIntegrity_returns_true_for_valid_invoice()
    {
        var invoice = CreateIssuedInvoice();
        Assert.True(invoice.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public void VerifyIntegrity_returns_false_for_tampered_invoice()
    {
        var original = CreateIssuedInvoice();

        var tampered = Invoice.Reconstitute(
            id: original.Id,
            number: original.Number,
            billingSource: original.BillingSource,
            issuer: original.Issuer,
            recipient: original.Recipient,
            issueDate: original.IssueDate,
            valueDate: null,
            createdAt: original.CreatedAt,
            lines: original.Lines.ToList(),
            appliedExchangeRate: original.AppliedExchangeRate,
            hash: "TAMPERED_HASH_VALUE_THAT_DOES_NOT_MATCH",
            previousHash: original.PreviousHash,
            status: original.Status,
            paymentReference: original.PaymentReference,
            rectifiedBy: null,
            notes: null);

        Assert.False(tampered.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public void VerifyIntegrity_uses_invoice_specific_hash_content()
    {
        var hasher = new Sha256InvoiceHasher();
        var invoice = CreateIssuedInvoice(hasher);
        Assert.True(invoice.VerifyIntegrity(hasher));
    }

    [Fact]
    public void VerifyIntegrity_returns_false_when_controlled_field_modified_after_issue()
    {
        var original = CreateIssuedInvoice();
        Assert.True(original.VerifyIntegrity(RealHasher));

        // Simulate DB tampering: a controlled field is changed but the stored hash stays the same.
        var tampered = Invoice.Reconstitute(
            id: original.Id,
            number: original.Number,
            billingSource: "tampered-source",
            issuer: original.Issuer,
            recipient: original.Recipient,
            issueDate: original.IssueDate,
            valueDate: null,
            createdAt: original.CreatedAt,
            lines: original.Lines.ToList(),
            appliedExchangeRate: original.AppliedExchangeRate,
            hash: original.Hash,
            previousHash: original.PreviousHash,
            status: original.Status,
            paymentReference: original.PaymentReference,
            rectifiedBy: null,
            notes: null);

        Assert.False(tampered.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public void VerifyIntegrity_returns_false_when_recipient_tax_id_modified()
    {
        var original = CreateIssuedInvoice();
        Assert.True(original.VerifyIntegrity(RealHasher));

        var differentRecipient = BillingParty.Create(
            legalName: original.Recipient.LegalName,
            taxId: TaxId.Create("X9999999Z", "ES"),   // different TaxId
            address: original.Recipient.Address,
            email: original.Recipient.Email);

        var tampered = Invoice.Reconstitute(
            id: original.Id, number: original.Number, billingSource: original.BillingSource,
            issuer: original.Issuer, recipient: differentRecipient,
            issueDate: original.IssueDate, valueDate: null, createdAt: original.CreatedAt,
            lines: original.Lines.ToList(), appliedExchangeRate: original.AppliedExchangeRate,
            hash: original.Hash, previousHash: original.PreviousHash,
            status: original.Status, paymentReference: original.PaymentReference,
            rectifiedBy: null, notes: null);

        Assert.False(tampered.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public void VerifyIntegrity_returns_false_when_line_quantity_modified()
    {
        var original = CreateIssuedInvoice();
        Assert.True(original.VerifyIntegrity(RealHasher));

        var originalLine = original.Lines[0];
        var tamperedLine = InvoiceLine.CreateInEur(
            originalLine.LineNumber,
            originalLine.Description,
            quantity: originalLine.Quantity + 1,   // quantity changed
            originalLine.UnitPriceEur,
            originalLine.TaxRate);

        var tampered = Invoice.Reconstitute(
            id: original.Id, number: original.Number, billingSource: original.BillingSource,
            issuer: original.Issuer, recipient: original.Recipient,
            issueDate: original.IssueDate, valueDate: null, createdAt: original.CreatedAt,
            lines: [tamperedLine], appliedExchangeRate: original.AppliedExchangeRate,
            hash: original.Hash, previousHash: original.PreviousHash,
            status: original.Status, paymentReference: original.PaymentReference,
            rectifiedBy: null, notes: null);

        Assert.False(tampered.VerifyIntegrity(RealHasher));
    }

    // ── BuildHashContent ───────────────────────────────────────────────────

    [Fact]
    public void BuildHashContent_invoice_type_is_F()
    {
        var invoice = CreateIssuedInvoice();
        Assert.Equal("F", invoice.BuildHashContent().InvoiceType);
    }

    [Fact]
    public void BuildHashContent_includes_payment_reference()
    {
        var invoice = InvoiceBuilder.BuildIssued(paymentReference: "PAY-XYZ");
        Assert.Equal("PAY-XYZ", invoice.BuildHashContent().PaymentReference);
    }
}
