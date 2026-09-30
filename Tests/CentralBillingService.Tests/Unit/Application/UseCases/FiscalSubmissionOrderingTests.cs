namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>
/// Chained authorities (AEAT VeriFactu) require records in stamping order and every stamped record
/// must end up reported. The submission drains pending predecessors first; the recovery job
/// re-enqueues the latest stalled record of each fiscal billing source.
/// </summary>
public class FiscalSubmissionOrderingTests
{
    private const string FiscalSource = "fiscal-source";
    private const string NonFiscalSource = "plain-source";

    private static BillingSourceRegistry BuildRegistry() => new BillingSourceRegistry(Options.Create(new CbsOptions
    {
        BillingSources =
        [
            new BillingSourceConfig { BillingSource = FiscalSource, Secret = "s", Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer()), Registrar = new RegistrarConfig { Type = "VeriFactu" } },
            new BillingSourceConfig { BillingSource = NonFiscalSource, Secret = "s", Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer()) },
        ]
    }));

    private static IInvoiceRepository RepositoryReturningInvoicesByNumber()
    {
        IInvoiceRepository repository = Substitute.For<IInvoiceRepository>();
        repository.FindByNumberAsync(FiscalSource, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => InvoiceBuilder.BuildIssued(serie: call.ArgAt<string>(1), billingSource: FiscalSource));
        return repository;
    }

    private static IFiscalRegistrarFactory FactoryReturning(IFiscalRegistrar registrar)
    {
        IFiscalRegistrarFactory factory = Substitute.For<IFiscalRegistrarFactory>();
        factory.GetFor(Arg.Any<BillingSourceConfig>()).Returns(registrar);
        return factory;
    }

    private static FiscalSubmissionOutcome Accepted() => new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, "CSV", "H", null, null);

    [Fact]
    public async Task Pending_predecessors_are_submitted_in_chain_order_before_the_invoice()
    {
        List<string> submittedSeries = new List<string>();
        IFiscalRegistrar registrar = Substitute.For<IFiscalRegistrar>();
        registrar.GetPendingSubmissionsUpToAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "INV-1", "INV-2", "INV-3" });
        registrar.SubmitAsync(Arg.Do<Invoice>(invoice => submittedSeries.Add(invoice.Number.Serie)), Arg.Any<CancellationToken>())
            .Returns(Accepted());
        SubmitFiscalRecordUseCase useCase = new SubmitFiscalRecordUseCase(
            RepositoryReturningInvoicesByNumber(), BuildRegistry(), FactoryReturning(registrar), Substitute.For<IIso9001>());

        await useCase.ExecuteAsync(new SubmitFiscalRecordCommand("INV-3", FiscalSource));

        Assert.Equal(new[] { "INV-1", "INV-2", "INV-3" }, submittedSeries);
    }

    [Fact]
    public async Task A_chain_mixing_invoices_and_rectificatives_submits_each_with_its_own_kind()
    {
        IInvoiceRepository repository = Substitute.For<IInvoiceRepository>();
        repository.FindByNumberAsync(FiscalSource, "INV-1", Arg.Any<CancellationToken>())
            .Returns(InvoiceBuilder.BuildIssued(serie: "INV-1", billingSource: FiscalSource));
        repository.FindByNumberAsync(FiscalSource, "REC-1", Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        RectificativeInvoice rectificative = InvoiceBuilder.BuildIssuedRectificative(serie: "REC", billingSource: FiscalSource);
        repository.FindRectificativeByNumberAsync(FiscalSource, "REC-1", Arg.Any<CancellationToken>()).Returns(rectificative);
        repository.FindByNumberAsync(FiscalSource, rectificative.OriginalInvoiceNumber.Value, Arg.Any<CancellationToken>())
            .Returns(InvoiceBuilder.BuildIssued(billingSource: FiscalSource));
        IFiscalRegistrar registrar = Substitute.For<IFiscalRegistrar>();
        registrar.GetPendingSubmissionsUpToAsync(FiscalSource, "REC-1", Arg.Any<CancellationToken>())
            .Returns(new List<string> { "INV-1", "REC-1" });
        registrar.SubmitAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()).Returns(Accepted());
        registrar.SubmitRectificativeAsync(Arg.Any<RectificativeInvoice>(), Arg.Any<RectifiedInvoiceAmounts>(), Arg.Any<CancellationToken>())
            .Returns(Accepted());
        SubmitFiscalRecordUseCase useCase = new SubmitFiscalRecordUseCase(
            repository, BuildRegistry(), FactoryReturning(registrar), Substitute.For<IIso9001>());

        await useCase.ExecuteAsync(new SubmitFiscalRecordCommand("REC-1", FiscalSource));

        Received.InOrder(() =>
        {
            registrar.SubmitAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
            registrar.SubmitRectificativeAsync(Arg.Any<RectificativeInvoice>(), Arg.Any<RectifiedInvoiceAmounts>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task A_rectificative_is_submitted_with_the_amounts_its_rectified_invoice_declared()
    {
        IInvoiceRepository repository = Substitute.For<IInvoiceRepository>();
        RectificativeInvoice rectificative = InvoiceBuilder.BuildIssuedRectificative(serie: "REC", billingSource: FiscalSource);
        Invoice rectifiedInvoice = InvoiceBuilder.BuildIssued(
            billingSource: FiscalSource, lines: [InvoiceBuilder.DefaultLine(1, unitPrice: 200m, taxRate: TaxRate.General)]);
        repository.FindRectificativeByNumberAsync(FiscalSource, "REC-1", Arg.Any<CancellationToken>()).Returns(rectificative);
        repository.FindByNumberAsync(FiscalSource, rectificative.OriginalInvoiceNumber.Value, Arg.Any<CancellationToken>())
            .Returns(rectifiedInvoice);
        IFiscalRegistrar registrar = Substitute.For<IFiscalRegistrar>();
        registrar.GetPendingSubmissionsUpToAsync(FiscalSource, "REC-1", Arg.Any<CancellationToken>()).Returns(new List<string>());
        registrar.SubmitRectificativeAsync(Arg.Any<RectificativeInvoice>(), Arg.Any<RectifiedInvoiceAmounts>(), Arg.Any<CancellationToken>())
            .Returns(Accepted());
        SubmitFiscalRecordUseCase useCase = new SubmitFiscalRecordUseCase(
            repository, BuildRegistry(), FactoryReturning(registrar), Substitute.For<IIso9001>());

        await useCase.ExecuteAsync(new SubmitFiscalRecordCommand("REC-1", FiscalSource));

        await registrar.Received(1).SubmitRectificativeAsync(
            rectificative,
            Arg.Is<RectifiedInvoiceAmounts>(amounts => amounts.TaxableBaseEur == 200m && amounts.TaxAmountEur == 42m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failing_predecessor_stops_the_drain_so_later_records_are_not_sent_first()
    {
        List<string> submittedSeries = new List<string>();
        IFiscalRegistrar registrar = Substitute.For<IFiscalRegistrar>();
        registrar.GetPendingSubmissionsUpToAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "INV-1", "INV-2" });
        registrar.SubmitAsync(Arg.Do<Invoice>(invoice => submittedSeries.Add(invoice.Number.Serie)), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("AEAT unavailable"));
        SubmitFiscalRecordUseCase useCase = new SubmitFiscalRecordUseCase(
            RepositoryReturningInvoicesByNumber(), BuildRegistry(), FactoryReturning(registrar), Substitute.For<IIso9001>());

        await Assert.ThrowsAsync<TimeoutException>(() => useCase.ExecuteAsync(new SubmitFiscalRecordCommand("INV-2", FiscalSource)));

        Assert.Equal(new[] { "INV-1" }, submittedSeries);
    }

    [Fact]
    public async Task Recovery_re_enqueues_the_latest_stalled_record_of_each_fiscal_source_only()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        IFiscalRegistrar registrar = Substitute.For<IFiscalRegistrar>();
        registrar.GetLatestStalledSubmissionAsync(FiscalSource, now - RetryStalledFiscalSubmissionsUseCase.StalledAfter, Arg.Any<CancellationToken>())
            .Returns("INV-7");
        IJobQueue jobQueue = Substitute.For<IJobQueue>();
        RetryStalledFiscalSubmissionsUseCase useCase = new RetryStalledFiscalSubmissionsUseCase(
            BuildRegistry(), FactoryReturning(registrar), jobQueue, clock);

        IReadOnlyList<string> reEnqueued = await useCase.ExecuteAsync();

        Assert.Equal(new[] { "INV-7" }, reEnqueued);
        await jobQueue.Received(1).EnqueueFiscalSubmissionAsync(
            Arg.Is<SubmitFiscalRecordCommand>(command => command.InvoiceNumber == "INV-7" && command.BillingSource == FiscalSource),
            Arg.Any<CancellationToken>());
        await registrar.DidNotReceive().GetLatestStalledSubmissionAsync(NonFiscalSource, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
