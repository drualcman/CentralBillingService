namespace CentralBillingService.Persistence.SqlServer.Options;

public class DatabaseOptions
{
    public const string SectionKey = "ConnectionStrings";

    public string CbsDb { get; set; }
}
