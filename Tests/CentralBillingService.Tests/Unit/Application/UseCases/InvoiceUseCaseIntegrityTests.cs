namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>
/// Verifies that use cases detect and throw InvoiceTamperingDetectedException
/// when the repository returns an invoice with a mismatched hash.
/// </summary>
public class InvoiceUseCaseIntegrityTests
{
    private readonly IInvoiceRepository _repository;
    private readonly FakeInvoiceHasher _hasher;

    public InvoiceUseCaseIntegrityTests()
    {
        _repository = Substitute.For<IInvoiceRepository>();
        _hasher = new FakeInvoiceHasher();
    }

    private static Invoice ValidInvoice(string serie = "TEST", int number = 1) =>
        InvoiceBuilder.BuildIssued(serie: serie, number: number, hasher: new FakeInvoiceHasher());

    private static Invoice TamperedInvoice(Invoice original) =>
        Invoice.Reconstitute(
            id: original.Id, number: original.Number, billingSource: original.BillingSource,
            issuer: original.Issuer, recipient: original.Recipient,
            issueDate: original.IssueDate, valueDate: null, createdAt: original.CreatedAt,
            lines: original.Lines.ToList(), appliedExchangeRate: original.AppliedExchangeRate,
            hash: "TAMPERED_HASH", previousHash: original.PreviousHash,
            status: original.Status, paymentReference: original.PaymentReference,
            rectifiedBy: null, notes: null);

    // ── GetInvoiceUseCase ──────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoice_returns_HasTamper_true_when_hash_is_invalid()
    {
        var useCase = new GetInvoiceUseCase(
            _repository,
            InvoiceBuilder.DefaultRegistry("web-test", "secret123"),
            _hasher);

        var tampered = TamperedInvoice(ValidInvoice());
        _repository.FindByNumberAsync("web-test", tampered.Number.Value, Arg.Any<CancellationToken>())
            .Returns(tampered);

        var result = await useCase.ExecuteAsync(new GetInvoiceQuery
        {
            BillingSource = "web-test",
            Secret = "secret123",
            InvoiceNumber = tampered.Number.Value
        });

        Assert.True(result.HasTamper);
    }

    [Fact]
    public async Task GetInvoice_succeeds_when_hash_is_valid()
    {
        var useCase = new GetInvoiceUseCase(
            _repository,
            InvoiceBuilder.DefaultRegistry("web-test", "secret123"),
            _hasher);

        var valid = ValidInvoice();
        _repository.FindByNumberAsync("web-test", valid.Number.Value, Arg.Any<CancellationToken>())
            .Returns(valid);

        var result = await useCase.ExecuteAsync(new GetInvoiceQuery
        {
            BillingSource = "web-test",
            Secret = "secret123",
            InvoiceNumber = valid.Number.Value
        });

        Assert.NotNull(result);
    }

    // ── ListInvoicesUseCase ────────────────────────────────────────────────

    [Fact]
    public async Task ListInvoices_marks_HasTamper_on_tampered_items()
    {
        var useCase = new ListInvoicesUseCase(
            _repository,
            InvoiceBuilder.DefaultRegistry("web-test", "secret123"),
            _hasher);

        var valid = ValidInvoice(serie: "A", number: 1);
        var tampered = TamperedInvoice(ValidInvoice(serie: "B", number: 2));

        _repository.ListAsync(Arg.Any<InvoiceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new InvoicePagedResult
            {
                Items = [valid, tampered],
                Rectificatives = [],
                TotalCount = 2,
                Page = 1,
                PageSize = 25
            });

        var result = await useCase.ExecuteAsync(new ListInvoicesQuery
        {
            BillingSource = "web-test",
            Secret = "secret123"
        });

        Assert.False(result.Items.First(x => x.InvoiceNumber == valid.Number.Value).HasTamper);
        Assert.True(result.Items.First(x => x.InvoiceNumber == tampered.Number.Value).HasTamper);
    }

    // ── RectifyInvoiceUseCase ──────────────────────────────────────────────

    [Fact]
    public async Task RectifyInvoice_throws_when_original_invoice_hash_is_invalid()
    {
        var registry = InvoiceBuilder.DefaultRegistry("web-test", "secret123");
        var domainService = new RectifyInvoiceService(registry, new FakeExchangeRateProvider(), _hasher);
        var eventDispatcher = Substitute.For<IInvoiceEventDispatcher>();

        var numberProviderFactory = Substitute.For<IInvoiceNumberProviderFactory>();
        var blobStorage = Substitute.For<IBlobStorageService>();
        blobStorage.GetQrUrl(Arg.Any<string>()).Returns("https://storage.test/qr/test.png");
        var useCase = new RectifyInvoiceUseCase(domainService, registry, _repository, eventDispatcher, _hasher, numberProviderFactory, blobStorage, Substitute.For<IIso9001>());

        var tampered = TamperedInvoice(ValidInvoice());
        _repository.FindByNumberAsync("web-test", tampered.Number.Value, Arg.Any<CancellationToken>())
            .Returns(tampered);

        await Assert.ThrowsAsync<InvoiceTamperingDetectedException>(() =>
            useCase.ExecuteAsync(new RectifyInvoiceCommand
            {
                BillingSource = "web-test",
                Secret = "secret123",
                OriginalInvoiceNumber = tampered.Number.Value,
                RectificativeSerie = "REC",
                Reason = "Test rectification reason long enough",
                RectificationType = RectificationType.Substitution,
                PaymentReference = "PAY-REC-001"
            }));
    }
}
