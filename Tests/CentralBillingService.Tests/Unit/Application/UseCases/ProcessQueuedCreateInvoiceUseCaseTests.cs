namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class ProcessQueuedCreateInvoiceUseCaseTests
{
    private readonly ICreateInvoiceUseCase _createUseCase;
    private readonly IInvoiceResultQueuePublisher _queuePublisher;
    private readonly IInvoiceResultCallbackNotifier _callbackNotifier;

    private static readonly MoneyResult ZeroEur = new() { Amount = 0m, CurrencyCode = "EUR", Formatted = "0,00 €" };
    private static readonly PartyResult FakeParty = new()
    {
        LegalName = "Test", DisplayName = "Test", TaxIdValue = "B12345678", TaxIdCountryCode = "ES",
        Email = "test@test.com", AddressLine1 = "Calle 1", City = "Madrid", PostalCode = "28001", AddressCountryCode = "ES"
    };
    private static readonly InvoiceResult FakeResult = new()
    {
        Id = Guid.Empty,
        InvoiceNumber = "FOTO2026-0001",
        BillingSource = "web-fotos",
        Status = "Issued",
        Issuer = FakeParty,
        Recipient = FakeParty,
        IssueDate = new DateOnly(2026, 1, 15),
        Lines = [],
        TaxableBaseEur = ZeroEur,
        TotalTaxAmountEur = ZeroEur,
        TotalEur = ZeroEur,
        TotalInOriginCurrency = ZeroEur,
        AppliedExchangeRate = new ExchangeRateResult
        {
            FromCurrency = "EUR", ToCurrency = "EUR", Rate = 1m,
            FetchedAt = DateTimeOffset.UtcNow, IsIdentity = true
        },
        Hash = "HASH",
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static readonly CreateInvoiceCommand BaseCommand = new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        Serie = "FOTO",
        OriginCurrencyCode = "EUR",
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
        Lines = [new InvoiceLineDto { Description = "Srv", Quantity = 1, UnitPrice = 100m, TaxRatePercentage = 21 }]
    };

    public ProcessQueuedCreateInvoiceUseCaseTests()
    {
        _createUseCase = Substitute.For<ICreateInvoiceUseCase>();
        _queuePublisher = Substitute.For<IInvoiceResultQueuePublisher>();
        _callbackNotifier = Substitute.For<IInvoiceResultCallbackNotifier>();

        _createUseCase
            .ExecuteAsync(Arg.Any<CreateInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(FakeResult);
    }

    private ProcessQueuedCreateInvoiceUseCase BuildUseCase(
        string billingSource = "web-fotos",
        string secret = "secret123",
        ResultQueueConfig? resultQueue = null,
        CallbackConfig? callback = null)
    {
        var config = new BillingSourceConfig
        {
            BillingSource = billingSource,
            Secret = secret,
            Issuer = InvoiceBuilder.DefaultIssuer(),
            ResultQueue = resultQueue,
            Callback = callback
        };
        var options = Options.Create(new CbsOptions { BillingSources = [config] });
        var registry = new BillingSourceRegistry(options);
        return new ProcessQueuedCreateInvoiceUseCase(_createUseCase, registry, _queuePublisher, _callbackNotifier);
    }

    [Fact]
    public async Task ExecuteAsync_returns_inner_use_case_result()
    {
        var useCase = BuildUseCase();

        var result = await useCase.ExecuteAsync(BaseCommand);

        Assert.Equal(FakeResult.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_to_queue_when_configured()
    {
        var queueConfig = new ResultQueueConfig
        {
            ConnectionString = "UseDevelopmentStorage=true",
            QueueName = "invoice-results"
        };
        var useCase = BuildUseCase(resultQueue: queueConfig);

        await useCase.ExecuteAsync(BaseCommand);

        await _queuePublisher.Received(1)
            .PublishAsync(FakeResult, queueConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_does_not_publish_when_queue_not_configured()
    {
        var useCase = BuildUseCase(); // no ResultQueue

        await useCase.ExecuteAsync(BaseCommand);

        await _queuePublisher.DidNotReceive()
            .PublishAsync(Arg.Any<InvoiceResult>(), Arg.Any<ResultQueueConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_notifies_callback_when_configured()
    {
        var callbackConfig = new CallbackConfig
        {
            Url = "https://client.example.com/invoice-webhook",
            AuthHeader = "X-Api-Key",
            AuthToken = "abc123"
        };
        var useCase = BuildUseCase(callback: callbackConfig);

        await useCase.ExecuteAsync(BaseCommand);

        await _callbackNotifier.Received(1)
            .NotifyAsync(FakeResult, callbackConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_does_not_notify_when_callback_not_configured()
    {
        var useCase = BuildUseCase(); // no Callback

        await useCase.ExecuteAsync(BaseCommand);

        await _callbackNotifier.DidNotReceive()
            .NotifyAsync(Arg.Any<InvoiceResult>(), Arg.Any<CallbackConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_returns_result_even_if_queue_publish_fails()
    {
        var queueConfig = new ResultQueueConfig
        {
            ConnectionString = "UseDevelopmentStorage=true",
            QueueName = "invoice-results"
        };
        var useCase = BuildUseCase(resultQueue: queueConfig);

        _queuePublisher
            .PublishAsync(Arg.Any<InvoiceResult>(), Arg.Any<ResultQueueConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Queue unavailable"));

        var result = await useCase.ExecuteAsync(BaseCommand);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_returns_result_even_if_callback_fails()
    {
        var callbackConfig = new CallbackConfig { Url = "https://client.example.com/webhook" };
        var useCase = BuildUseCase(callback: callbackConfig);

        _callbackNotifier
            .NotifyAsync(Arg.Any<InvoiceResult>(), Arg.Any<CallbackConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Callback unreachable"));

        var result = await useCase.ExecuteAsync(BaseCommand);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_to_queue_and_notifies_callback_when_both_configured()
    {
        var queueConfig = new ResultQueueConfig
        {
            ConnectionString = "UseDevelopmentStorage=true",
            QueueName = "invoice-results"
        };
        var callbackConfig = new CallbackConfig { Url = "https://client.example.com/webhook" };
        var useCase = BuildUseCase(resultQueue: queueConfig, callback: callbackConfig);

        await useCase.ExecuteAsync(BaseCommand);

        await _queuePublisher.Received(1)
            .PublishAsync(Arg.Any<InvoiceResult>(), Arg.Any<ResultQueueConfig>(), Arg.Any<CancellationToken>());
        await _callbackNotifier.Received(1)
            .NotifyAsync(Arg.Any<InvoiceResult>(), Arg.Any<CallbackConfig>(), Arg.Any<CancellationToken>());
    }
}
