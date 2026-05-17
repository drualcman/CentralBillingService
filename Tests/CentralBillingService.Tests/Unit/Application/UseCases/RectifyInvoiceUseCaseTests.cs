namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class RectifyInvoiceUseCaseTests
{
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceEventDispatcher _eventDispatcher;
    private readonly IInvoiceNumberProvider _numberProvider;
    private readonly IInvoiceNumberProviderFactory _numberProviderFactory;
    private readonly RectifyInvoiceUseCase _useCase;

    public RectifyInvoiceUseCaseTests()
    {
        var registry = InvoiceBuilder.DefaultRegistry("web-fotos", "secret123");
        var domainService = new RectifyInvoiceService(registry, new FakeExchangeRateProvider(), new FakeInvoiceHasher());

        _repository = Substitute.For<IInvoiceRepository>();
        _eventDispatcher = Substitute.For<IInvoiceEventDispatcher>();
        _numberProvider = Substitute.For<IInvoiceNumberProvider>();
        _numberProviderFactory = Substitute.For<IInvoiceNumberProviderFactory>();

        _numberProviderFactory.GetFor(Arg.Any<BillingSourceConfig>()).Returns(_numberProvider);
        _numberProvider
            .ReserveNextNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(1);
        _repository
            .GetLastHashAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        _useCase = new RectifyInvoiceUseCase(domainService, registry, _repository, _eventDispatcher, new FakeInvoiceHasher(), _numberProviderFactory);
    }

    private static RectifyInvoiceCommand BuildCommand(
        string originalNumber = "TEST2026-0001",
        RectificationType type = RectificationType.Substitution) =>
        new()
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            OriginalInvoiceNumber = originalNumber,
            RectificativeSerie = "REC",
            Reason = "Error en los datos del cliente",
            RectificationType = type,
            PaymentReference = "PAY-002"
        };

    [Fact]
    public async Task ExecuteAsync_returns_both_original_and_rectificative()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(original);

        var result = await _useCase.ExecuteAsync(BuildCommand());

        Assert.NotNull(result.UpdatedOriginal);
        Assert.NotNull(result.Rectificative);
    }

    [Fact]
    public async Task ExecuteAsync_original_is_marked_as_rectified()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(original);

        var result = await _useCase.ExecuteAsync(BuildCommand());

        Assert.Equal("Rectified", result.UpdatedOriginal.Status);
    }

    [Fact]
    public async Task ExecuteAsync_saves_both_invoices_atomically()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(original);

        await _useCase.ExecuteAsync(BuildCommand());

        await _repository.Received(1)
            .SaveRectificativeAsync(
                Arg.Any<RectificativeInvoice>(),
                Arg.Any<Invoice>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_dispatches_rectified_event()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(original);

        await _useCase.ExecuteAsync(BuildCommand());

        await _eventDispatcher.Received(1)
            .InvoiceRectifiedAsync(
                Arg.Any<RectificativeInvoice>(),
                Arg.Any<Invoice>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_invoice_not_found_throws()
    {
        _repository
            .FindByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        await Assert.ThrowsAsync<InvoiceNotFoundException>(() => _useCase.ExecuteAsync(BuildCommand()));
    }

    [Fact]
    public async Task ExecuteAsync_empty_original_number_throws_argument_exception()
    {
        var command = new RectifyInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            OriginalInvoiceNumber = "",
            RectificativeSerie = "REC",
            Reason = "Error en los datos del cliente",
            RectificationType = RectificationType.Substitution,
            PaymentReference = "PAY-002"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_empty_rectificative_serie_throws_argument_exception()
    {
        var command = new RectifyInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            OriginalInvoiceNumber = "TEST2026-0001",
            RectificativeSerie = "",
            Reason = "Error en los datos del cliente",
            RectificationType = RectificationType.Substitution,
            PaymentReference = "PAY-002"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_short_reason_throws_argument_exception()
    {
        var command = new RectifyInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            OriginalInvoiceNumber = "TEST2026-0001",
            RectificativeSerie = "REC",
            Reason = "Corto",   // less than 10 chars
            RectificationType = RectificationType.Substitution,
            PaymentReference = "PAY-002"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_difference_without_lines_throws_argument_exception()
    {
        var command = new RectifyInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            OriginalInvoiceNumber = "TEST2026-0001",
            RectificativeSerie = "REC",
            Reason = "Descuento no aplicado correctamente",
            RectificationType = RectificationType.Difference,
            Lines = null,   // required for Difference
            PaymentReference = "PAY-002"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_event_failure_does_not_fail_use_case()
    {
        var original = InvoiceBuilder.BuildIssued(serie: "TEST", number: 1, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "TEST2026-0001", Arg.Any<CancellationToken>())
            .Returns(original);

        _eventDispatcher
            .InvoiceRectifiedAsync(Arg.Any<RectificativeInvoice>(), Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Bus no disponible"));

        var result = await _useCase.ExecuteAsync(BuildCommand());
        Assert.NotNull(result);
    }
}
