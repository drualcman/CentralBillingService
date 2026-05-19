using CentralBillingService.VerifyUI;
using CentralBillingService.VerifyUI.Services;
using DigitalDoor.Reporting.Entities.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The CBS API base URL is configured in wwwroot/appsettings.json.
// Set it to the deployed Azure Function URL, e.g. "https://cbs-api.azurewebsites.net".
// CORS must allow requests from this app's origin on the CBS API side.
var apiBaseUrl = builder.Configuration["CbsApiBaseUrl"]
    ?? throw new InvalidOperationException("CbsApiBaseUrl is not configured in appsettings.json.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddTransient<IReportAsBytes, FakePdfGenerator>();
builder.Services.AddReportingBlazorServices();

await builder.Build().RunAsync();
