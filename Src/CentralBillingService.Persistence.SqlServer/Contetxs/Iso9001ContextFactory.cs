namespace CentralBillingService.Persistence.SqlServer.Contetxs;
/// <summary>
/// Add-Migration InitialCreate -p CentralBillingService.Persistence.SqlServer -s CentralBillingService.Persistence.SqlServer -c Iso9001Context  -o Migrations/Iso9001
/// Update-Database -p CentralBillingService.Persistence.SqlServer -s CentralBillingService.Persistence.SqlServer -context Iso9001Context
/// </summary>
internal class Iso9001ContextFactory : IDesignTimeDbContextFactory<Iso9001Context>
{
    public Iso9001Context CreateDbContext(string[] args)
    {
        IOptions<DatabaseOptions> DBOptions =
            Microsoft.Extensions.Options.Options.Create(
            new DatabaseOptions
            {
                //copy here the conection string you want to use when apply some mgration
                Iso9001Db = "Server=(localdb)\\MSSQLLocalDB;Database=iso9001db;Trusted_Connection=True;MultipleActiveResultSets=true"
            });
        return new Iso9001Context(DBOptions);
    }
}
