namespace CentralBillingService.Tests.Integration;

public sealed class Iso9001DatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=cbsiso9001_tests;Trusted_Connection=True;MultipleActiveResultSets=true";

    public IOptions<DatabaseOptions> Options { get; } =
        Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { Iso9001Db = ConnectionString });

    public async Task InitializeAsync()
    {
        await using var ctx = new Iso9001Context(Options);
        await ctx.Database.EnsureCreatedAsync();
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM IncidentReports");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
