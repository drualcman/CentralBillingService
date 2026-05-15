using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CentralBillingService.VerifyUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The CBS API base URL is configured in wwwroot/appsettings.json.
// Set it to the deployed Azure Function URL, e.g. "https://cbs-api.azurewebsites.net".
// CORS must allow requests from this app's origin on the CBS API side.
var apiBaseUrl = builder.Configuration["CbsApiBaseUrl"]
    ?? throw new InvalidOperationException("CbsApiBaseUrl is not configured in appsettings.json.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

await builder.Build().RunAsync();
