var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
.AddEnvironmentVariables()
#if DEBUG
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
.AddUserSecrets<Program>()
#endif
;
builder.Services
    .AddBillingDomain(
        options => builder.Configuration.GetSection(CbsOptions.SectionKey).Bind(options),
        mail => builder.Configuration.GetSection(EmailOptions.SectionKey).Bind(mail)
    )
    .AddBillingApplication()
    .AddBillingInfrastructure()
    .AddSqlServerPersistence(
        options => builder.Configuration.GetSection(DatabaseOptions.SectionKey).Bind(options)
    )
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var host = builder.Build();

await host.RunAsync();
