# Central Billing Service (CBS)

An extensible billing service for compliant electronic invoicing — serverless API on Azure Functions, WPF desktop management console, and a .NET client SDK. Ships with a VeriFactu (Spain) verification implementation out of the box; other countries can be supported by implementing the provided interfaces.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                        Consumers                             │
│   CentralBillingService.Client  ──►  AzureFunction.API      │
└──────────────────────────────────────────────────────────────┘
                                             │
                         ┌───────────────────┼───────────────────┐
                         ▼                   ▼                   ▼
                   Application         Infrastructure       Persistence
                      Layer               Layer            (SQL Server)
                         │                   │
                         └────── Domain ─────┘

┌──────────────────────────────────────────────────────────────┐
│                   Central Control (WPF)                      │
│   Direct access to Application + Infrastructure layers       │
└──────────────────────────────────────────────────────────────┘
```

---

## Projects

### Core — `CentralBillingService.AzureFunction.API`
The main backend, deployed as an Azure Functions v4 app. Exposes a REST API for all billing operations:

| Endpoint | Description |
|---|---|
| `POST /invoices` | Create a new invoice |
| `GET /invoices/{id}` | Retrieve an invoice by ID |
| `GET /invoices` | List invoices with filtering |
| `POST /invoices/rectify` | Issue a rectificative invoice |
| `GET /invoices/verify` | Verify invoice integrity |

Also handles queue-triggered asynchronous invoice processing.

---

### Consumer Client — `CentralBillingService.Client`
A .NET SDK (targets net9.0 and net10.0) for services that want to interact with CBS. Drop it into any .NET application and call the API without dealing with HTTP details.

```csharp
// Register in DI
services.AddCbsClient(options =>
{
    options.BaseUrl = "https://your-cbs-instance.azurewebsites.net";
    options.ApiKey  = "your-api-key";
});

// Inject and use
public class MyService(ICbsService cbs)
{
    public async Task IssueInvoice()
    {
        var result = await cbs.CreateInvoiceAsync(new CreateInvoiceCommand { ... });
    }
}
```

---

### Central Control — `CentralBillingService.WPF`
A Windows desktop application (WPF, Material Design) for billing administrators. Provides a UI to manage billing sources, clients, products, series, and invoices. Connects directly to the same backend layers as the API.

---

### Supporting Projects

| Project | Role |
|---|---|
| `CentralBillingService.Domain` | Core entities, aggregates, and domain rules |
| `CentralBillingService.Application` | Use cases and application interfaces |
| `CentralBillingService.Infrastructure` | External services, hashing, verification providers, queue publishing |
| `CentralBillingService.Persistence.SqlServer` | EF Core SQL Server implementation |

---

## Extending CBS — Adding a Country

Invoice verification is fully abstracted. To support a new country or regulation, implement `IInvoiceVerificationUrlProvider` from the Domain project and register your implementation in DI:

```csharp
public class MyCountryVerificationUrlProvider : IInvoiceVerificationUrlProvider
{
    public string GetVerificationUrl(Invoice invoice) => $"https://tax.mycountry.gov/verify/{invoice.Id}";
}

// In Program.cs / startup
services.AddScoped<IInvoiceVerificationUrlProvider, MyCountryVerificationUrlProvider>();
```

No changes to the core are needed. See `SpanishAeatVerificationUrlProvider` in the Infrastructure project as a reference implementation.

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (local or remote)
- Azure Functions Core Tools v4 (for local API runs)
- Azure Storage Emulator / Azurite (for queue processing)

### Run the API locally
```bash
cd Src/CentralBillingService.AzureFunction.API
dotnet user-secrets set "ConnectionStrings:Sql" "Server=.;Database=CBS;Trusted_Connection=True;"
func start
```

### Run the desktop app
```bash
cd Src/CentralBillingService.WPF
dotnet run
```

### Run tests
```bash
cd Tests/CentralBillingService.Tests
dotnet test
```

---

## License

See [LICENSE](LICENSE). Source code is provided for transparency and verification. Compiled binaries are free to use. Implementing the public interfaces to extend CBS for your country or use case is explicitly permitted.
