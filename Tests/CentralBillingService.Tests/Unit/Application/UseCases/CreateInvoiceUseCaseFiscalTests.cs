namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>
/// Verifies the fiscal-registrar wiring in CreateInvoiceUseCase: when a billing source is
/// configured with a real registrar (Registrar.Type != "None"), the created invoice is
/// stamped and an async submission is enqueued; when it is "None", neither happens.
/// </summary>
public class CreateInvoiceUseCaseFiscalTests
{
    private const string Source = "web-fiscal";
    private const string Secret = "secret123";

    private sealed class StubRegistrar : IFiscalRegistrar
    {
        public string RegistrarType => "VeriFactu";
        public int StampCalls;
        public Task<FiscalStampResult> StampAsync(Invoice invoice, CancellationToken ct = default)
        {
            StampCalls++;
            return Task.FromResult(new FiscalStampResult("AEAT-HUELLA", "https://aeat/qr"));
        }
        public Task<FiscalSubmissionOutcome> SubmitAsync(Invoice invoice, CancellationToken ct = default) =>
            Task.FromResult(new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, "CSV", "AEAT-HUELLA", null, null));
        public Task<FiscalStampResult> StampRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken ct = default) =>
            Task.FromResult(FiscalStampResult.None);
        public Task<FiscalSubmissionOutcome> SubmitRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken ct = default) =>
            Task.FromResult(new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, null, null, null, null));
        public Task<IReadOnlyList<string>> GetPendingSubmissionsUpToAsync(string billingSource, string invoiceNumber, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        public Task<string?> GetLatestStalledSubmissionAsync(string billingSource, DateTimeOffset stampedBefore, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);
    }

    private static (CreateInvoiceUseCase useCase, IInvoiceRepository repo, IJobQueue queue, StubRegistrar registrar)
        Build(string registrarType)
    {
        var config = new BillingSourceConfig
        {
            BillingSource = Source,
            Secret = Secret,
            Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer()),
            Registrar = new RegistrarConfig { Type = registrarType }
        };
        var registry = new BillingSourceRegistry(Options.Create(new CbsOptions { BillingSources = [config] }));
        var domainService = new CreateInvoiceService(registry, new FakeExchangeRateProvider(), new FakeInvoiceHasher());

        var repo = Substitute.For<IInvoiceRepository>();
        repo.GetLastHashAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var numberProvider = Substitute.For<IInvoiceNumberProvider>();
        numberProvider.ReserveNextNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(1);
        var numberProviderFactory = Substitute.For<IInvoiceNumberProviderFactory>();
        numberProviderFactory.GetFor(Arg.Any<BillingSourceConfig>()).Returns(numberProvider);

        var registrar = new StubRegistrar();
        var registrarFactory = Substitute.For<IFiscalRegistrarFactory>();
        registrarFactory.GetFor(Arg.Any<BillingSourceConfig>()).Returns(registrar);

        var queue = Substitute.For<IJobQueue>();
        var blob = Substitute.For<IBlobStorageService>();
        blob.GetQrUrl(Arg.Any<string>()).Returns("https://storage.test/qr.png");

        var useCase = new CreateInvoiceUseCase(
            domainService, registry, repo, Substitute.For<IInvoiceEventDispatcher>(),
            numberProviderFactory, registrarFactory, queue, blob, Substitute.For<IIso9001>());

        return (useCase, repo, queue, registrar);
    }

    private static CreateInvoiceCommand Command() => new()
    {
        BillingSource = Source,
        Secret = Secret,
        Serie = "FAC",
        OriginCurrencyCode = "EUR",
        PaymentMethod = "card",
        PaymentReference = "PAY-FISCAL-1",
        Recipient = new RecipientDto
        {
            LegalName = "ACME SL", TaxIdValue = "B12345678", TaxIdCountryCode = "ES",
            Email = "c@acme.com", AddressLine1 = "C/ 1", City = "Madrid",
            PostalCode = "28001", AddressCountryCode = "ES"
        },
        Lines = [new InvoiceLineDto { Description = "Servicio", Quantity = 1, UnitPrice = 100m, TaxRatePercentage = 21 }]
    };

    [Fact]
    public async Task VeriFactu_source_stamps_and_enqueues_submission()
    {
        var (useCase, repo, queue, registrar) = Build("VeriFactu");

        await useCase.ExecuteAsync(Command());

        Assert.Equal(1, registrar.StampCalls);
        await repo.Received(1).UpdateFiscalStampAsync(Arg.Any<Guid>(), "AEAT-HUELLA", Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await queue.Received(1).EnqueueFiscalSubmissionAsync(
            Arg.Is<SubmitFiscalRecordCommand>(c => c.BillingSource == Source), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task None_source_does_not_stamp_or_enqueue()
    {
        var (useCase, repo, queue, registrar) = Build(RegistrarConfig.None);

        await useCase.ExecuteAsync(Command());

        Assert.Equal(0, registrar.StampCalls);
        await repo.DidNotReceive().UpdateFiscalStampAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueFiscalSubmissionAsync(Arg.Any<SubmitFiscalRecordCommand>(), Arg.Any<CancellationToken>());
    }
}
