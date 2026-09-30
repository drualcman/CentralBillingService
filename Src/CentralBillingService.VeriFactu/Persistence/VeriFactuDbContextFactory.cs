using Microsoft.EntityFrameworkCore.Design;

namespace CentralBillingService.VeriFactu.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations` can build the context without the host.
/// The connection string is irrelevant for scaffolding migrations.
/// Add-Migration InitialCreate -p CentralBillingService.VeriFactu -s CentralBillingService.VeriFactu -c VeriFactuDbContext  
/// Update-Database -p CentralBillingService.VeriFactu -s CentralBillingService.VeriFactu -context VeriFactuDbContext

/// </summary>
public sealed class VeriFactuDbContextFactory : IDesignTimeDbContextFactory<VeriFactuDbContext>
{
    public VeriFactuDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VeriFactuDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=cbsdb;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options;
        return new VeriFactuDbContext(options);
    }
}
