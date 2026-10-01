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
                CbsDb = "Server=tcp:sergiortizgomez.database.windows.net,1433;Initial Catalog=cbs;Persist Security Info=False;User ID=drualcman;Password=kW6vT27z*5081+69;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

            });
        return new SqlInvoiceWriteContext(DBOptions);
    }
}
