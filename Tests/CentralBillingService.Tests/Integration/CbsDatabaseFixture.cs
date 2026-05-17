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
        await ctx.Database.MigrateAsync();
        // Clean data from previous test runs
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM InvoiceLines");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Invoices");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM InvoiceSequences");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
