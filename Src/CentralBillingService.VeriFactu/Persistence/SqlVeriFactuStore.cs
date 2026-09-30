namespace CentralBillingService.VeriFactu.Persistence;

public sealed class SqlVeriFactuStore : IVeriFactuStore
{
    private readonly VeriFactuDbContext _db;

    public SqlVeriFactuStore(VeriFactuDbContext db) => _db = db;

    public async Task<string> StampAsync(
        string nif, string billingSource, string invoiceNumber,
        string newNumSerie, string newFechaExpedicion,
        Func<ChainLink, StampComputation> compute,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Idempotency: a retried creation must not advance the chain twice.
            var existing = await _db.Submissions
                .FirstOrDefaultAsync(x => x.BillingSource == billingSource && x.InvoiceNumber == invoiceNumber, cancellationToken);
            if (existing is not null)
                return existing.Huella;

            // Pessimistic, per-chain lock (range-locks the key when the row does not exist yet,
            // serializing the "first record" insert). Other chains run in parallel.
            var chain = await _db.Chains
                .FromSqlInterpolated($@"SELECT * FROM [VeriFactuChain] WITH (UPDLOCK, HOLDLOCK) WHERE [Nif] = {nif} AND [BillingSource] = {billingSource}")
                .FirstOrDefaultAsync(cancellationToken);

            var previous = new ChainLink(chain?.LastNumSerie, chain?.LastFechaExpedicion, chain?.LastHuella);

            var computation = compute(previous);

            if (chain is null)
            {
                chain = new VeriFactuChainEntity { Nif = nif, BillingSource = billingSource, Sequence = 1 };
                _db.Chains.Add(chain);
            }
            else
            {
                chain.Sequence++;
            }

            chain.LastNumSerie = newNumSerie;
            chain.LastFechaExpedicion = newFechaExpedicion;
            chain.LastHuella = computation.Huella;

            _db.Submissions.Add(new VeriFactuSubmissionEntity
            {
                Id = Guid.NewGuid(),
                BillingSource = billingSource,
                InvoiceNumber = invoiceNumber,
                IssuerNif = nif,
                State = (int)FiscalSubmissionState.Pending,
                Huella = computation.Huella,
                PreviousHuella = previous.Huella,
                PreviousNumSerie = previous.NumSerie,
                PreviousFechaExpedicion = previous.FechaExpedicion,
                FechaHoraGenRegistro = computation.FechaHoraGenRegistro,
                CreatedUtc = DateTimeOffset.UtcNow,
            });

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return computation.Huella;
        });
    }

    public Task<VeriFactuSubmissionEntity?> GetSubmissionAsync(
        string billingSource, string invoiceNumber, CancellationToken cancellationToken = default) =>
        _db.Submissions.FirstOrDefaultAsync(
            x => x.BillingSource == billingSource && x.InvoiceNumber == invoiceNumber, cancellationToken);

    public async Task UpdateSubmissionAsync(VeriFactuSubmissionEntity entity, CancellationToken cancellationToken = default)
    {
        _db.Submissions.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
