var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddBillingDomain(
        options => builder.Configuration.GetSection(CbsOptions.SectionKey).Bind(options)
    )
    .AddBillingApplication()
    .AddBillingInfrastructure()
    .AddSqlServerPersistence(
        options => builder.Configuration.GetSection(DatabaseOptions.SectionKey).Bind(options)
    );

var host = builder.Build();

await host.RunAsync();
