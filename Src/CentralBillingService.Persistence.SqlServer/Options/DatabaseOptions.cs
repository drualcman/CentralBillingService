namespace CentralBillingService.Persistence.SqlServer.Options;

public class DatabaseOptions
{
    public const string SectionKey = "ConnectionStrings";

    public string CbsDb1 { get; set; }
    public string CbsDb { get; set; }
    public string Iso9001Db1 { get; set; }
    public string Iso9001Db { get; set; }
}
