namespace CentralBillingService.Persistence.SqlServer.Contetxs;
/// <summary>
/// Add-Migration InitialCreate -p CentralBillingService.Persistence.SqlServer -s CentralBillingService.Persistence.SqlServer -c SqlInvoiceWriteContext  -o Migrations/SqlInvoiceContext
/// Update-Database -p CentralBillingService.Persistence.SqlServer -s CentralBillingService.Persistence.SqlServer -context SqlInvoiceWriteContext
/// </summary>
internal class SqlInvoiceWriteContextFactory : IDesignTimeDbContextFactory<SqlInvoiceWriteContext>
{
    public SqlInvoiceWriteContext CreateDbContext(string[] args)
    {
        IOptions<DatabaseOptions> DBOptions =
            Microsoft.Extensions.Options.Options.Create(
            new DatabaseOptions
            {
                //copy here the conection string you want to use when apply some mgration
                CbsDb = "Server=(localdb)\\MSSQLLocalDB;Database=cbsdb;Trusted_Connection=True;MultipleActiveResultSets=true"

            });
        return new SqlInvoiceWriteContext(DBOptions);
    }
}
