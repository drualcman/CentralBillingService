namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class VerifyInvoiceIntegrityUseCaseTests
{
    private readonly IInvoiceRepository _repository;
    private readonly FakeInvoiceHasher _hasher;
    private readonly VerifyInvoiceIntegrityUseCase _useCase;

    public VerifyInvoiceIntegrityUseCaseTests()
    {
        _repository = Substitute.For<IInvoiceRepository>();
        _hasher = new FakeInvoiceHasher();
        var registry = InvoiceBuilder.DefaultRegistry("web-test", "secret123");
        _useCase = new VerifyInvoiceIntegrityUseCase(_repository, registry, _hasher);
    }

    private static VerifyInvoiceQuery Query(
        string invoiceNumber = "TEST2026-0001",
        string providedHash = "PLACEHOLDER_HASH") => new()
    {
        BillingSource = "web-test",
        InvoiceNumber = invoiceNumber,
        ProvidedHash = providedHash
    };

    private static Invoice TamperedInvoice(Invoice original) =>
        Invoice.Reconstitute(
            id: original.Id, number: original.Number, billingSource: original.BillingSource,
            issuer: original.Issuer, recipient: original.Recipient,
            issueDate: original.IssueDate, valueDate: null, createdAt: original.CreatedAt,
            lines: original.Lines.ToList(), appliedExchangeRate: original.AppliedExchangeRate,
            hash: "TAMPERED_HASH", previousHash: original.PreviousHash,
            status: original.Status, paymentReference: original.PaymentReference,
            rectifiedBy: null, notes: null);

    [Fact]
    public async Task ExecuteAsync_returns_valid_result_for_intact_invoice()
    {
        var invoice = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, hasher: _hasher);
        _repository
            .FindByNumberAsync("web-test", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(invoice);

        var result = await _useCase.ExecuteAsync(Query(providedHash: invoice.Hash));

        Assert.True(result.IsValid);
        Assert.Equal("TEST2026-0001", result.InvoiceNumber);
        Assert.Equal(invoice.Hash, result.Hash);
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_result_when_hash_does_not_match()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, hasher: _hasher);
        var tampered = TamperedInvoice(original);
        _repository
            .FindByNumberAsync("web-test", tampered.Number.Value, Arg.Any<CancellationToken>())
            .Returns(tampered);

        // ProvidedHash matches the stored (tampered) hash so documentHashMatches=true,
        // but integrity recomputation will fail, exposing the DB tampering.
        var result = await _useCase.ExecuteAsync(Query(providedHash: "TAMPERED_HASH"));

        Assert.False(result.IsValid);
        Assert.Equal("TEST2026-0001", result.InvoiceNumber);
        Assert.Equal("TAMPERED_HASH", result.Hash);
        Assert.Contains("tampering", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_checks_rectificative_when_regular_not_found()
    {
        var rectificative = InvoiceBuilder.BuildIssuedRectificative(serie: "REC", number: 1, hasher: _hasher);
        _repository
            .FindByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repository
            .FindRectificativeByNumberAsync("web-test", "REC2026-0001", Arg.Any<CancellationToken>())
            .Returns(rectificative);

        var result = await _useCase.ExecuteAsync(Query("REC2026-0001", rectificative.Hash));

        Assert.True(result.IsValid);
        Assert.Equal(rectificative.Hash, result.Hash);
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_for_tampered_rectificative()
    {
        var valid = InvoiceBuilder.BuildIssuedRectificative(serie: "REC", number: 1, hasher: _hasher);
        var tampered = RectificativeInvoice.Reconstitute(
            id: valid.Id, number: valid.Number, billingSource: valid.BillingSource,
            originalNumber: valid.OriginalInvoiceNumber, originalIssueDate: valid.OriginalIssueDate,
            rectificationReason: valid.RectificationReason, rectificationType: valid.RectificationType,
            issuer: valid.Issuer, recipient: valid.Recipient,
            issueDate: valid.IssueDate, createdAt: valid.CreatedAt,
            lines: valid.Lines.ToList(), appliedExchangeRate: valid.AppliedExchangeRate,
            hash: "TAMPERED_REC_HASH", previousHash: null,
            status: valid.Status, paymentReference: valid.PaymentReference, notes: null);

        _repository
            .FindByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repository
            .FindRectificativeByNumberAsync("web-test", tampered.Number.Value, Arg.Any<CancellationToken>())
            .Returns(tampered);

        var result = await _useCase.ExecuteAsync(Query(tampered.Number.Value, "TAMPERED_REC_HASH"));

        Assert.False(result.IsValid);
        Assert.Equal("TAMPERED_REC_HASH", result.Hash);
    }

    [Fact]
    public async Task ExecuteAsync_throws_not_found_when_neither_type_exists()
    {
        _repository
            .FindByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);
        _repository
            .FindRectificativeByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RectificativeInvoice?)null);

        await Assert.ThrowsAsync<InvoiceNotFoundException>(() =>
            _useCase.ExecuteAsync(Query()));
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_billing_source_is_empty()
    {
        var query = new VerifyInvoiceQuery
        {
            BillingSource = "",
            InvoiceNumber = "TEST2026-0001",
            ProvidedHash = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_invoice_number_is_empty()
    {
        var query = new VerifyInvoiceQuery
        {
            BillingSource = "web-test",
            InvoiceNumber = "",
            ProvidedHash = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(query));
    }
}
