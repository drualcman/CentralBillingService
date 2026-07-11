namespace CentralBillingService.Tests.Integration;

/// <summary>
/// Applies migrations against the dedicated integration-test database (cbsdb_tests)
/// and leaves the data there after the run so you can inspect it in SSMS / Azure Data Studio.
/// </summary>
public sealed class CbsDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=cbsdb_tests;Trusted_Connection=True;MultipleActiveResultSets=true";

    public IOptions<DatabaseOptions> Options { get; } =
        Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { CbsDb = ConnectionString });

    public async Task InitializeAsync()
    {
        await using var ctx = new SqlInvoiceWriteContext(Options);

        // Clean data from previous runs BEFORE applying migrations: a newly-added unique index
        // (e.g. on BillingSource + PaymentReference) would otherwise fail to create against rows
        // left by an older run made under the pre-index schema. Guarded so a first-ever run,
        // where the schema does not exist yet, simply skips the cleanup and creates it below.
        try
        {
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM InvoiceLines");
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Invoices");
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM InvoiceSequences");
        }
        catch
        {
            // Schema not created yet (first run) — nothing to clean.
        }

        await ctx.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
