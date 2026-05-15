namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class CreateInvoiceUseCaseTests
{
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceEventDispatcher _eventDispatcher;
    private readonly IInvoiceNumberProvider _numberProvider;
    private readonly IInvoiceNumberProviderFactory _numberProviderFactory;
    private readonly CreateInvoiceUseCase _useCase;

    public CreateInvoiceUseCaseTests()
    {
        var registry = InvoiceBuilder.DefaultRegistry("web-fotos", "secret123");
        var domainService = new CreateInvoiceService(registry, new FakeExchangeRateProvider(), new FakeInvoiceHasher());

        _repository = Substitute.For<IInvoiceRepository>();
        _eventDispatcher = Substitute.For<IInvoiceEventDispatcher>();
        _numberProvider = Substitute.For<IInvoiceNumberProvider>();
        _numberProviderFactory = Substitute.For<IInvoiceNumberProviderFactory>();

        var blobStorage = Substitute.For<IBlobStorageService>();
        blobStorage.GetBlobUrl(Arg.Any<string>())
            .Returns("https://storage.test/qr/test.png");

        var qrJobQueue = Substitute.For<IQrCodeJobQueue>();

        _numberProviderFactory.GetFor(Arg.Any<BillingSourceConfig>()).Returns(_numberProvider);
        _numberProvider
            .ReserveNextNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(1);
        _repository
            .GetLastHashAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        _useCase = new CreateInvoiceUseCase(
            domainService, registry, _repository, _eventDispatcher,
            _numberProviderFactory, blobStorage, qrJobQueue);
    }

    private static CreateInvoiceCommand BuildCommand(string currencyCode = "EUR") => new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        Serie = "FOTO",
        OriginCurrencyCode = currencyCode,
        PaymentMethod = "card",
        PaymentReference = "PAY-001",
        Recipient = new RecipientDto
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
            new InvoiceLineDto
            {
                Description = "Servicio fotográfico",
                Quantity = 1,
                UnitPrice = 100m,
                TaxRatePercentage = 21
            }
        ]
    };

    [Fact]
    public async Task ExecuteAsync_returns_invoice_result_with_correct_totals()
    {
        var result = await _useCase.ExecuteAsync(BuildCommand());

        Assert.NotNull(result);
        Assert.Equal("FOTO2026-0001", result.InvoiceNumber);
        Assert.Equal("web-fotos", result.BillingSource);
        Assert.Equal(100m, result.TaxableBaseEur.Amount);
        Assert.Equal(21m, result.TotalTaxAmountEur.Amount);
        Assert.Equal(121m, result.TotalEur.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_saves_invoice_to_repository()
    {
        await _useCase.ExecuteAsync(BuildCommand());

        await _repository.Received(1)
            .SaveAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_dispatches_invoice_created_event()
    {
        await _useCase.ExecuteAsync(BuildCommand());

        await _eventDispatcher.Received(1)
            .InvoiceCreatedAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_uses_reserved_number_for_invoice_number()
    {
        _numberProvider
            .ReserveNextNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(7);

        var result = await _useCase.ExecuteAsync(BuildCommand());

        Assert.Equal("FOTO2026-0007", result.InvoiceNumber);
    }

    [Fact]
    public async Task ExecuteAsync_event_failure_does_not_fail_use_case()
    {
        _eventDispatcher
            .InvoiceCreatedAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Bus no disponible"));

        var result = await _useCase.ExecuteAsync(BuildCommand());
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_empty_billing_source_throws_argument_exception()
    {
        var command = new CreateInvoiceCommand
        {
            BillingSource = "",
            Secret = "secret123",
            Serie = "FOTO",
            OriginCurrencyCode = "EUR",
            PaymentMethod = "card",
            PaymentReference = "PAY-001",
            Recipient = BuildCommand().Recipient,
            Lines = BuildCommand().Lines
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_empty_serie_throws_argument_exception()
    {
        var command = new CreateInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            Serie = "",
            OriginCurrencyCode = "EUR",
            PaymentMethod = "card",
            PaymentReference = "PAY-001",
            Recipient = BuildCommand().Recipient,
            Lines = BuildCommand().Lines
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_empty_lines_throws_argument_exception()
    {
        var command = new CreateInvoiceCommand
        {
            BillingSource = "web-fotos",
            Secret = "secret123",
            Serie = "FOTO",
            OriginCurrencyCode = "EUR",
            PaymentMethod = "card",
            PaymentReference = "PAY-001",
            Recipient = BuildCommand().Recipient,
            Lines = []
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_reserves_number_with_correct_parameters()
    {
        await _useCase.ExecuteAsync(BuildCommand());

        await _numberProvider.Received(1).ReserveNextNumberAsync(
            "web-fotos",
            "FOTO",
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}
