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
    .AddBillingInfrastructure(
        iso9001 => builder.Configuration.GetSection(Iso9001ClientOptions.SectionKey).Bind(iso9001)
    )
    .AddSqlServerPersistence(
        options => builder.Configuration.GetSection(DatabaseOptions.SectionKey).Bind(options)
    )
    // VeriFactu fiscal registrar (AEAT). Selected only for billing sources with Registrar.Type = "VeriFactu".
    // Each such source carries its own certificate/endpoint in its Registrar.Settings (no global config).
    // Its own EF context reuses the CbsDb database for the huella chain + submission state.
    .AddVeriFactuRegistrar(
        builder.Configuration.GetConnectionString("CbsDb") ?? string.Empty
    )
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var host = builder.Build();

await host.RunAsync();
